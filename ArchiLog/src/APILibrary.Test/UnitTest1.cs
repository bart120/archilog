using APILibrary.Test.Mock;
using APILibrary.Test.Mock.Models;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication.Controllers;

namespace APILibrary.Test
{
    public class Tests
    {
        private MockDbContext _db;
        private PizzasController _controller;

        [SetUp]
        public void Setup()
        {
            _db = MockDbContext.GetDbContext();
            _controller = new PizzasController(_db);
        }

        [Test]
        public async Task Test1()
        {
            var actionResult = await _controller.GetAllAsync("", "", "");
            var result = actionResult.Result as ObjectResult;
            var values = ((IEnumerable<object>)(result).Value);

            Assert.AreEqual((int)System.Net.HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual(_db.Pizzas.Count(), values.Count());
        }


        [Test]
        public void Test2()
        {
            Assert.Pass();
        }
    }
}