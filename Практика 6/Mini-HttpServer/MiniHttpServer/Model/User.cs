using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Model
{
    internal class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastName {  get; set; }
        public int Age {get;set;}
        public string Password { get; set; }
        public string Email { get; set; }


    }
}
