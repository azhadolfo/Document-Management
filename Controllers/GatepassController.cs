﻿using Document_Management.Data;
using Document_Management.Hubs;
using Document_Management.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.Web.CodeGeneration;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Document_Management.Controllers
{
    public class GatepassController : Controller
    {
        //public IActionResult Index()
        //{
        //    return View();
        //}

        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        private readonly IHubContext<NotificationHub> _notificationHub;

        //Passing the dbcontext in to another variable
        public GatepassController(ApplicationDbContext context, IHubContext<NotificationHub> notification)
        {
            _dbcontext = context;
            _notificationHub = notification;
        }

        //RequestGatepass
        [HttpGet]
        public IActionResult Insert()
        {
            var username = HttpContext.Session.GetString("username");
            if (!string.IsNullOrEmpty(username))
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Insert(RequestGP gpInfo)
        {
            var username = HttpContext.Session.GetString("username");

            if (ModelState.IsValid)
            {
                if (username != null)
                {
                    gpInfo.Username = username;
                }
                _dbcontext.Gatepass.Add(gpInfo);
                gpInfo.Status = "Pending";
                _dbcontext.SaveChanges();
                TempData["success"] = "Request created successfully";
                var hubConnections = _dbcontext.HubConnections.Where(h => h.Username == "leo").ToList();
                foreach (var hubConnection in hubConnections)
                {
                    await _notificationHub.Clients.Client(hubConnection.ConnectionId).SendAsync("ReceivedPersonalNotification", "You have a new request", gpInfo.Username);
                }
                return RedirectToAction("Insert");
            }

            return View(gpInfo);
        }

        public IActionResult Validator()
        {
            var username = HttpContext.Session.GetString("userrole")?.ToLower();
            if (!(username == "validator"))
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

            ViewBag.users = _dbcontext.Gatepass
                .OrderByDescending(user => user.Id)
                    .ToList();

            return View();
        }

        [HttpGet]
        public IActionResult Approved(int? id)
        {
            var requestGP = _dbcontext.Gatepass.FirstOrDefault(x => x.Id == id);

            if (requestGP == null)
            {
                return NotFound();
            }

            return View(requestGP);
        }

        [HttpPost, ActionName("Approved")]
        public async Task<IActionResult> Approved(int id)
        {
            if (_dbcontext.Gatepass == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Account'  is null.");
            }
            var client = await _dbcontext.Gatepass.FindAsync(id);
            //if (client != null)
            //{
            //    _dbcontext.Gatepass.Remove(client);
            //}

            if (client != null)
            {
                client.Status = "Approved";
                await _dbcontext.SaveChangesAsync();
                TempData["success"] = "Approved successfully";
                var hubConnections = _dbcontext.HubConnections.Where(h => h.Username == client.Username).ToList();
                foreach (var hubConnection in hubConnections)
                {
                    await _notificationHub.Clients.Client(hubConnection.ConnectionId).SendAsync("ReceivedPersonalNotification", "Your request has been approved", client.Username);
                }
            }
            return RedirectToAction(nameof(Validator));
        }

        [HttpGet]
        public IActionResult Disapproved(int? id)
        {
            var requestGP = _dbcontext.Gatepass.FirstOrDefault(x => x.Id == id);

            if (requestGP == null)
            {
                return NotFound();
            }

            return View(requestGP);
        }

        [HttpPost, ActionName("Disapproved")]
        public async Task<IActionResult> Disapproved(int id)
        {
            if (_dbcontext.Gatepass == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Account'  is null.");
            }
            var client = await _dbcontext.Gatepass.FindAsync(id);
            //if (client != null)
            //{
            //    _dbcontext.Gatepass.Remove(client);
            //}

            if (client != null)
            {
                client.Status = "Disapproved";
                await _dbcontext.SaveChangesAsync();
                TempData["success"] = "Disapproved successfully";
                var hubConnections = _dbcontext.HubConnections.Where(h => h.Username == client.Username).ToList();
                foreach (var hubConnection in hubConnections)
                {
                    await _notificationHub.Clients.Client(hubConnection.ConnectionId).SendAsync("ReceivedPersonalNotification", "Your request has been disapproved", client.Username);
                }
            }
            return RedirectToAction(nameof(Validator));
        }

        [HttpGet]
        public IActionResult RecievedGP()
        {
            var username = HttpContext.Session.GetString("username");

            if (!string.IsNullOrEmpty(username))
            {
                ViewBag.users = _dbcontext.Gatepass
                    .OrderByDescending(user => user.Id)
                    .ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Generate(int id, RequestGP ifRead)
        {
            var requestGP = _dbcontext.Gatepass.FirstOrDefault(x => x.Id == id);

            if (requestGP == null)
            {
                return NotFound();
            }

            if (ifRead.IsRead != true)
            {
                requestGP.IsRead = true;
                await _dbcontext.SaveChangesAsync();
            }

            return View(requestGP);
        }

        [HttpPost]
        public IActionResult Generate(string inputText)
        {
            inputText = "https://www.youtube.com/watch?v=4Vq1F5tP_68&t=197s";
            using (MemoryStream ms = new MemoryStream())
            {
                QRCodeGenerator qRCodeGenerator = new QRCodeGenerator();
                QRCodeData qRCodeData = qRCodeGenerator.CreateQrCode(inputText, QRCodeGenerator.ECCLevel.Q);
                QRCode qRCode = new QRCode(qRCodeData);
                using (Bitmap oBitmap = qRCode.GetGraphic(20))
                {
                    oBitmap.Save(ms, ImageFormat.Png);
                    ViewBag.QrCode = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                }
            }
            return View();
        }
    }
}