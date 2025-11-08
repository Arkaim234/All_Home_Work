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

        /// <summary>
        /// Получить всех пользователей
        /// GET /user
        /// </summary>
        [HttpGet("user")]
        public IActionResult GetUsers()
        {
            var users = _context.ReadByAll<User>("Users");
            return Json(users);
        }

        /// <summary>
        /// Получить пользователя по Id
        /// GET /user/{id}
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
        /// Получить пользователей старше указанного возраста (с использованием Where)
        /// GET /user/age/{minAge}
        /// </summary>
        [HttpGet("user/age/{minAge}")]
        public IActionResult GetUsersByAge(int minAge)
        {
            // Используем метод Where с LINQ-выражением
            var users = _context.Where<User>(x => x.Age > minAge, "Users");
            return Json(users);
        }

        /// <summary>
        /// Получить первого пользователя с указанным email (с использованием FirstOrDefault)
        /// GET /user/email/{email}
        /// </summary>
        [HttpGet("user/email/{email}")]
        public IActionResult GetUserByEmail(string email)
        {
            // Используем метод FirstOrDefault с LINQ-выражением
            var user = _context.FirstOrDefault<User>(x => x.Email == email, "Users");

            if (user == null)
            {
                Context.Response.StatusCode = 404;
                return Json(new { message = "User with this email not found" });
            }

            return Json(user);
        }

        /// <summary>
        /// Получить пользователей по имени
        /// GET /user/name/{name}
        /// </summary>
        [HttpGet("user/name/{name}")]
        public IActionResult GetUsersByName(string name)
        {
            var users = _context.Where<User>(x => x.Name == name, "Users");
            return Json(users);
        }

        /// <summary>
        /// Получить пользователей в указанном возрастном диапазоне
        /// GET /user/agerange?min={minAge}&max={maxAge}
        /// </summary>
        [HttpGet("user/agerange")]
        public IActionResult GetUsersByAgeRange(int min, int max)
        {
            // Пример с более сложным условием
            var users = _context.Where<User>(x => x.Age >= min && x.Age <= max, "Users");
            return Json(users);
        }

        /// <summary>
        /// Создать нового пользователя
        /// POST /user/create
        /// Body: { "Name": "Иван", "LastName": "Иванов", "Age": 25, "Email": "ivan@example.com", "Password": "12345" }
        /// </summary>
        [HttpPost("user/create")]
        public IActionResult CreateUser(User user)
        {
            var createdUser = _context.Create(user, "Users");
            return Json(createdUser);
        }

        /// <summary>
        /// Обновить пользователя
        /// POST /user/update/{id}
        /// Body: { "Name": "Новое имя", "LastName": "Новая фамилия", "Age": 26, "Email": "new@example.com", "Password": "newpass" }
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
        /// POST /user/delete/{id}
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