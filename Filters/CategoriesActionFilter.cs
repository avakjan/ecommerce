using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using OnlineShoppingSite.Models;

namespace OnlineShoppingSite.Filters
{
    public class CategoriesActionFilter : IActionFilter
    {
        private const string CacheKey = "CategoriesList";

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var dbContext = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();

            List<Category> categories;

            if (!cache.TryGetValue(CacheKey, out categories))
            {
                // Key not in cache, so get data from database
                categories = dbContext.Categories.ToList();

                // Set cache options
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                // Save data in cache
                cache.Set(CacheKey, categories, cacheOptions);
            }

            // Store categories in ViewBag
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                controller.ViewBag.Categories = categories;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do nothing
        }
    }
}