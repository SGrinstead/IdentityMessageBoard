using IdentityMessageBoard.DataAccess;
using Microsoft.AspNetCore.Mvc;
using IdentityMessageBoard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace IdentityMessageBoard.Controllers
{
    [Authorize(Roles = "Super User,Admin")]
    public class UsersController : Controller
    {
        private readonly MessageBoardContext _context;

        public UsersController(MessageBoardContext context)
        {
            _context = context;
        }

        [Route("/users/{userId}/allmessages")]
        public IActionResult AllMessages(string userId)
        {
            var user = _context.Users.Find(userId);
            if(user is null)
            {
                return NotFound();
            }
            var allMessages = new Dictionary<string, List<Message>>()
            {
                { "active" , new List<Message>() },
                { "expired", new List<Message>() }
            };
            foreach (var message in _context.Messages.Include(m => m.Author).Where(m => m.Author == user))
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
    }
}
