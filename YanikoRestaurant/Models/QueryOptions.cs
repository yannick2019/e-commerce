using System.Linq.Expressions;

namespace YanikoRestaurant.Models
{
    public class QueryOptions<T> where T : class
    {
        public Expression<Func<T, object>>? OrderBy { get; set; }

        public Expression<Func<T, bool>>? Where { get; set; }

        private List<string> includes = new List<string>();

        public string Includes
        {
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    includes = value.Split(',')
                        .Select(i => i.Trim())
                        .Where(i => !string.IsNullOrWhiteSpace(i))
                        .ToList();
                }
                else
                {
                    includes.Clear();
                }
            }
        }

        public IEnumerable<string> GetIncludes() => includes;

        public bool HasWhere => Where != null;

        public bool HasOrderBy => OrderBy != null;
    }
}