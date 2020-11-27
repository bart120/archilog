using APILibrary.Core.Attributes;
using APILibrary.Core.Extensions;
using APILibrary.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace APILibrary.Core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class ControllerBaseAPI<TModel, TContext> : ControllerBase where TModel : ModelBase where TContext : DbContext
    {
        protected readonly TContext _context;

        public ControllerBaseAPI(TContext context)
        {
            this._context = context;
        }

        //?fields=email,phone



        [ProducesResponseType((int)HttpStatusCode.OK)]
        [HttpGet]
        public virtual async Task<ActionResult<IEnumerable<dynamic>>> GetAllAsync([FromQuery] string fields, [FromQuery] string asc, [FromQuery] string range)
        {
            var query = _context.Set<TModel>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(asc))
            {
                query = query.OrderBy(x => x.ID);
            }

            if (!string.IsNullOrWhiteSpace(range))
            {
                query = query.Skip(10);
            }

            if (!string.IsNullOrWhiteSpace(fields))
            {
                var tab = fields.Split(',');

                // var results = await IQueryableExtensions.SelectDynamic<TModel>(query, tab).ToListAsync();
                var results = await query.SelectDynamic(tab).ToListAsync();

                return results.Select((x) => IQueryableExtensions.SelectObject(x, tab)).ToList();
            }
            else
            {      
                return Ok( ToJsonList(await query.ToListAsync()));
            }

        }

        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("{id}")]
        public virtual async Task<ActionResult<TModel>> GetById([FromRoute] int id, [FromQuery] string fields)
        {
            var query = _context.Set<TModel>().AsQueryable();
            //solution 2: optimisation de la requete SQL

            if (!string.IsNullOrWhiteSpace(fields))
            {
                var tab = new List<string>(fields.Split(','));
                if (!tab.Contains("id"))
                    tab.Add("id");
                var result = query.SelectModel(tab.ToArray()).SingleOrDefault(x => x.ID == id);
                if (result != null)
                {
                    var tabFields = fields.Split(',');
                    return Ok(IQueryableExtensions.SelectObject(result, tabFields));
                }
                else
                {
                    return NotFound(new { Message = $"ID {id} not found" });
                }
            }
            else
            {
                var result = query.SingleOrDefault(x => x.ID == id);
                if (result != null)
                {
                    
                    return Ok(ToJson(result));
                }
                else
                {
                    return NotFound(new { Message = $"ID {id} not found" });
                }
            }
        }


        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost]
        public virtual async Task<ActionResult<TModel>> CreateItem([FromBody] TModel item)
        {
            if (ModelState.IsValid)
            {
                _context.Add(item);
                await _context.SaveChangesAsync();
                return Created("", ToJson(item));
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPut("{id}")]
        public virtual async Task<ActionResult<TModel>> UpdateItem([FromRoute] int id, [FromBody] TModel item)
        {
            if (id != item.ID)
            {
                return BadRequest();
            }

            bool result = await _context.Set<TModel>().AnyAsync(x => x.ID == id);
            if (!result)
                return NotFound(new { Message = $"ID {id} not found" });

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update<TModel>(item);
                    await _context.SaveChangesAsync();
                    return Ok(ToJson(item));
                }
                catch (Exception e)
                {
                    return BadRequest(new { e.Message });
                }
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpDelete("{id}")]
        public virtual async Task<ActionResult<TModel>> DeleteItem([FromRoute] int id)
        {
            TModel item = await _context.Set<TModel>().FindAsync(id);
            if (item == null)
                return NotFound();

            _context.Remove<TModel>(item);

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(new { e.Message });
            }

            /*int result = await _context.SaveChangesAsync();
            if(result != 0)
            {
                return NoContent();
            }
            else
            {
                return BadRequest();
            }*/
        }



        protected IEnumerable<dynamic> ToJsonList(IEnumerable<dynamic> tab)
        {
            var tabNew = tab.Select((x) =>
            {
                return ToJson(x);
            });
            return tabNew;
        }

        protected dynamic ToJson(dynamic item)
        {
            var expo = new ExpandoObject() as IDictionary<string, object>;

            var collectionType = typeof(TModel);

            IDictionary<string, object> dico = item as IDictionary<string, object>;
            if (dico != null)
            {
                foreach (var propDyn in dico)
                {
                    var propInTModel = collectionType.GetProperty(propDyn.Key, BindingFlags.Public |
                            BindingFlags.IgnoreCase | BindingFlags.Instance);

                    var isPresentAttribute = propInTModel.CustomAttributes
                    .Any(x => x.AttributeType == typeof(NotJsonAttribute));

                    if (!isPresentAttribute)
                        expo.Add(propDyn.Key, propDyn.Value);
                }
            }
            else
            {
                foreach (var prop in collectionType.GetProperties())
                {
                    var isPresentAttribute = prop.CustomAttributes
                    .Any(x => x.AttributeType == typeof(NotJsonAttribute));

                    if (!isPresentAttribute)
                        expo.Add(prop.Name, prop.GetValue(item));
                }
            }
            return expo;
        }
    }
}
