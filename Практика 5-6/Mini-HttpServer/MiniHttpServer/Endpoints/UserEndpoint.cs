using MiniHttpServer.Frimework.Core;
using MiniHttpServer.Frimework.Core.Atributes;
using MiniHttpServer.Frimework.Core.HttpResponse;
using MiniHttpServer.Frimework.Settings;
using MiniHttpServer.Model;
using MyORMLibrary;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    internal class UserEndpoint : EndpointBase
    {
        private readonly ORMContext _context;

        public UserEndpoint()
        {
            _context = new ORMContext(Singleton.GetInstance().Settings.ConectionString);
        }

        [HttpGet("user")]
        public IActionResult GetUsers()
        {
            var users = _context.ReadByAll<User>("Users");
            return Json(users);
        }

        /// <summary>
        /// Получить пользователя по Id
        /// GET /users/{id}
        /// </summary>
        [HttpGet("user/{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _context.ReadById<User>(id, "Users");

            if (user == null)
            {
                Context.Response.StatusCode = 404;
                return Json(new { message = "User not found" });
            }

            return Json(user);
        }

        /// <summary>
        /// Создать нового пользователя
        /// POST /users/create
        /// </summary>
        [HttpPost("user/create")]
        public IActionResult CreateUser(User user)
        {
            var createdUser = _context.Create(user, "Users");
            return Json(createdUser);
        }

        /// <summary>
        /// Обновить пользователя
        /// POST /users/update/{id}
        /// </summary>
        [HttpPost("user/update/{id}")]
        public IActionResult UpdateUser(int id, User user)
        {
            var existingUser = _context.ReadById<User>(id, "Users");

            if (existingUser == null)
            {
                Context.Response.StatusCode = 404;
                return Json(new { message = "User not found" });
            }

            _context.Update(id, user, "Users");
            return Json(new { message = "User updated successfully" });
        }

        /// <summary>
        /// Удалить пользователя
        /// POST /users/delete/{id}
        /// </summary>
        [HttpPost("user/delete/{id}")]
        public IActionResult DeleteUser(int id)
        {
            var existingUser = _context.ReadById<User>(id, "Users");

            if (existingUser == null)
            {
                Context.Response.StatusCode = 404;
                return Json(new { message = "User not found" });
            }

            _context.Delete(id, "Users");
            return Json(new { message = "User deleted successfully" });
        }
    }
}
