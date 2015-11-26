using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Formatting = System.Xml.Formatting;

namespace BootgridMvcConnector.Controllers
{
    public class HomeController : System.Web.Mvc.Controller
    {
        public static List<PersonObject> PersonObjects;

        public HomeController()
        {
            PersonObjects = new List<PersonObject>();
            for (var i = 1; i <= 100; i++)
            {
                var personObject = new PersonObject()
                {
                    Firstname = string.Format("Person First Name {0}",i),
                    Lastname = string.Format("Person Last Name {0}", i),
                    Id = i
                };
                PersonObjects.Add(personObject);
            }
        }

        public ActionResult Index()
        {
            return View();
        }

        public JsonResult Get(GridCommand<PersonObject> gridComman)
        {
            var db = PersonObjects.AsQueryable();
            var dataSource = db.ToDataSourceResult(gridComman);
            return Json(dataSource);
        }
    }

    public class GridCommand<T>
    {
        public int Current { get; set; }

        public int RowCount { get; set; }

        public string SearchPhrase { get; set; }

        public ExpandoObject Sort { get; set; }
    }

    public class PersonObject
    {
        public int Id { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }
    }

    public static class GridCommandExtension
    {
        public static string ToDataSourceResult<T>(this IQueryable<T> list, GridCommand<T> gridCommand)
        {
            if (gridCommand.Sort != null)
            {
                var propertyInfos = typeof(T).GetProperties();
                foreach (var item in propertyInfos)
                {
                    var name = item.Name.ToString().ToLower();
                    var byName = (IDictionary<string, object>)gridCommand.Sort;
                    object tryId = "";
                    var boolTryId = byName.TryGetValue(name, out tryId);
                    if (boolTryId)
                    {
                        if (tryId is string[])
                        {
                            var stringtryId = (string[])tryId;
                            if (stringtryId.Length > 0)
                            {
                                var direction = stringtryId[0];
                                if (direction == "asc")
                                {
                                    list = list.Order(item.Name, SortDirection.Ascending);
                                }
                                else if (direction == "desc")
                                {
                                    list = list.Order(item.Name, SortDirection.Descending);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var propertyInfos = typeof(T).GetProperties();
                list = list.Order(propertyInfos[0].Name, SortDirection.Ascending);
            }

            var refinedList = list.Skip(gridCommand.RowCount * (gridCommand.Current - 1)).Take(gridCommand.RowCount).ToList();
            var data = new
            {
                current = gridCommand.Current,
                rowCount = gridCommand.RowCount,
                rows = refinedList,
                total = 100
            };

            string refinedListJson =
                 JsonConvert.SerializeObject(
                 data,
                 Newtonsoft.Json.Formatting.Indented,
                 new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }
                 );
            return refinedListJson;
        }

        private static IOrderedQueryable<T> Order<T>(
            this IQueryable<T> source,
            string propertyName,
            SortDirection descending,
            bool anotherLevel = false)
        {
            var param = Expression.Parameter(typeof(T), string.Empty);
            var property = Expression.PropertyOrField(param, propertyName);
            var sort = Expression.Lambda(property, param);
            var call = Expression.Call(
                typeof(Queryable),
                (!anotherLevel ? "OrderBy" : "ThenBy") +
                (descending == SortDirection.Descending ? "Descending" : string.Empty),
                new[] { typeof(T), property.Type },
                source.Expression,
                Expression.Quote(sort));
            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
        }
    }
}