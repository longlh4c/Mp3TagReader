// -----------------------------------------------------------------------
// <copyright file="MySortableBindingList.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Mp3TagReader.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    /// reference http://www.codeproject.com/Articles/31418/Implementing-a-Sortable-BindingList-Very-Very-Quic
    public class MySortableBindingList<T> : BindingList<T>
    {
        private ListSortDirection sortDirection;
        private PropertyDescriptor sortProperty;
        private bool isSorted;

        // items matching this stay at the top in both sort directions (e.g. the ".." row)
        public Predicate<T> KeepOnTop { get; set; }

        // function that refereshes the contents
        // of the base classes collection of elements
        private Action<MySortableBindingList<T>, List<T>>
                       populateBaseList = (a, b) => a.ResetItems(b);

        // a cache of functions that perform the sorting
        // for a given type, property, and sort direction
        private static Dictionary<string, Func<List<T>, IEnumerable<T>>>
           cachedOrderByExpressions = new Dictionary<string, Func<List<T>,
                                                     IEnumerable<T>>>();

        public MySortableBindingList()
        {
        }

        public MySortableBindingList(IEnumerable<T> enumerable)
        {
            populateBaseList(this, enumerable.ToList());
        }

        public MySortableBindingList(List<T> list)
        {
            populateBaseList(this, list);
        }

        protected override void ApplySortCore(PropertyDescriptor prop,
                                ListSortDirection direction)
        {
            /*
             Look for an appropriate sort method in the cache if not found .
             Call CreateOrderByMethod to create one.
             Apply it to the current items.
             Notify any bound controls that the sort has been applied.
             */

            sortProperty = prop;
            sortDirection = direction;

            var orderByMethodName = direction ==
                ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";
            var cacheKey = typeof(T).GUID + prop.Name + orderByMethodName;

            if (!cachedOrderByExpressions.ContainsKey(cacheKey))
            {
                CreateOrderByMethod(prop, orderByMethodName, cacheKey);
            }

            List<T> sorted = cachedOrderByExpressions[cacheKey](this.Items.ToList()).ToList();
            if (KeepOnTop != null)
            {
                List<T> top = sorted.Where(x => KeepOnTop(x)).ToList();
                sorted = top.Concat(sorted.Where(x => !KeepOnTop(x))).ToList();
            }
            ResetItems(sorted);
            isSorted = true;
            ResetBindings();
        }

        private void CreateOrderByMethod(PropertyDescriptor prop,
                     string orderByMethodName, string cacheKey)
        {
            /*
             Create a generic method implementation for IEnumerable<T>.
             Cache it.
            */

            var sourceParameter = Expression.Parameter(typeof(List<T>), "source");
            var lambdaParameter = Expression.Parameter(typeof(T), "lambdaParameter");
            var accesedMember = typeof(T).GetProperty(prop.Name);
            var propertySelectorLambda =
                Expression.Lambda(Expression.MakeMemberAccess(lambdaParameter,
                                  accesedMember), lambdaParameter);
            var orderByMethod = typeof(Enumerable).GetMethods()
                                           .Where(a => a.Name == orderByMethodName &&
                                                        a.GetParameters().Length == 2)
                                           .Single()
                                           .MakeGenericMethod(typeof(T), prop.PropertyType);

            var orderByExpression = Expression.Lambda<Func<List<T>, IEnumerable<T>>>(
                                        Expression.Call(orderByMethod,
                                                new Expression[] { sourceParameter,
                                                               propertySelectorLambda }),
                                                sourceParameter);

            cachedOrderByExpressions.Add(cacheKey, orderByExpression.Compile());
        }

        protected override void RemoveSortCore()
        {
            isSorted = false;
            sortProperty = null;
            ResetBindings();
        }

        protected override void ClearItems()
        {
            isSorted = false;
            sortProperty = null;
            base.ClearItems();
        }

        private void ResetItems(List<T> items)
        {
            // refill silently; callers raise a single Reset notification afterwards
            bool raiseEvents = RaiseListChangedEvents;
            RaiseListChangedEvents = false;
            try
            {
                base.ClearItems();

                for (int i = 0; i < items.Count; i++)
                {
                    base.InsertItem(i, items[i]);
                }
            }
            finally
            {
                RaiseListChangedEvents = raiseEvents;
            }
        }

        protected override bool IsSortedCore
        {
            get
            {
                return isSorted;
            }
        }

        protected override bool SupportsSortingCore
        {
            get
            {
                // indeed we do
                return true;
            }
        }

        protected override ListSortDirection SortDirectionCore
        {
            get
            {
                return sortDirection;
            }
        }

        protected override PropertyDescriptor SortPropertyCore
        {
            get
            {
                return sortProperty;
            }
        }
    }
}
