using IdentityMessageBoard.DataAccess;
using IdentityMessageBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.AspNetCore.Identity;

namespace IdentityMessageBoard.Controllers
{
    public class MessagesController : Controller
    {
        private readonly MessageBoardContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(MessageBoardContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var messages = _context.Messages
                .Include(m => m.Author)
                .OrderBy(m => m.ExpirationDate)
                .ToList()
                .Where(m => m.IsActive()); // LINQ Where(), not EF Where()

            return View(messages);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AllMessages()
        {
            var allMessages = new Dictionary<string, List<Message>>()
            {
                { "active" , new List<Message>() },
                { "expired", new List<Message>() }
            };

            foreach (var message in _context.Messages.Include(m => m.Author))
            {
                if (message.IsActive())
                {
                    allMessages["active"].Add(message);
                }
                else
                {
                    allMessages["expired"].Add(message);
                }
            }


            return View(allMessages);
        }

        [Authorize]
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public IActionResult Create(string userId, string content, int expiresIn)
        {
            var user = _context.ApplicationUsers.Find(userId);

            _context.Messages.Add(
                new Message()
                {
                    Content = content,
                    ExpirationDate = DateTime.UtcNow.AddDays(expiresIn),
                    Author = user
                });

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [Route("/users/{userId}/messages/{messageId}/edit")]
        public IActionResult Edit(string userId, int messageId)
        {
            var message = _context.Messages
                .Where(m => m.Id == messageId)
                .Include(m => m.Author)
                .First();

            if (userId != message.Author.Id) return BadRequest();
            if (message is null) return NotFound();
            return View(message);
        }

        [HttpPost]
        [Route("/users/{userId}/messages/{messageId}/update")]
        public IActionResult Update(string userId, int messageId, int expiresIn, Message message)
        {
            if (!ModelState.IsValid) return BadRequest();
            if (userId != _userManager.GetUserId(User)) return BadRequest();
            
            message.Id = messageId;
            message.ExpirationDate = DateTime.UtcNow.AddDays(expiresIn);
            _context.Messages.Update(message);
            _context.SaveChanges();

            return Redirect($"/users/{userId}/allmessages");
        }

        [Route("/users/{userId}/messages/{messageId}/delete")]
        public IActionResult Delete(string userId, int messageId)
        {
            if (userId != _userManager.GetUserId(User)) return BadRequest();

            var messageToDelete = _context.Messages.Find(messageId);
            if (messageToDelete is null) return NotFound();
            _context.Remove(messageToDelete);
            _context.SaveChanges();

            return Redirect($"/users/{userId}/allmessages");
        }
    }
}
