using APILibrary.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Models
{
    public class Pizza : ModelBase
    {
        //[Key]
        //public int ID { get; set; }

        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Topping { get; set; }
    }
}
