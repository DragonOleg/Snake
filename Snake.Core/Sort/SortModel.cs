using System.Collections.Generic;
using System.Linq;

namespace Snake.Core
{
    public class SortModel
    {
        private const string CONST_ArrowUpClass = "fas fa-arrow-up";
        private const string CONST_ArrowDownClass = "fas fa-arrow-down";

        private IList<SortableColumn> sortableColumns = new List<SortableColumn>();
        public string SortedProperty { get; set; }
        public SortOrder SortOrder { get; set; }

        public void AddColumn(string columnName, bool isDefaultColumn = false)
        {
            SortableColumn tmp = sortableColumns.SingleOrDefault(c => c.ColumnName.ToLower() == columnName.ToLower());

            if (tmp == null)
            {
                sortableColumns.Add(new SortableColumn() { ColumnName = columnName.ToLower() });
            }

            if (isDefaultColumn || sortableColumns.Count == 1)
            {
                SortedProperty = columnName;
                SortOrder = SortOrder.Ascending;
            }
        }

        public SortableColumn GetColumn(string columnName)
        {
            SortableColumn tmp = sortableColumns.SingleOrDefault(c => c.ColumnName.ToLower() == columnName.ToLower());

            if (tmp == null)
            {
                sortableColumns.Add(new SortableColumn() { ColumnName = columnName });
            }

            return tmp;
        }

        public void ApplySort(string sortExpression)
        {
            if (string.IsNullOrEmpty(sortExpression))
            {
                sortExpression = SortedProperty;
            }

            sortExpression = sortExpression.ToLower();

            foreach (SortableColumn column in sortableColumns)
            {
                column.SortIcon = "";
                column.SortExpression = column.ColumnName;

                if (sortExpression == column.ColumnName.ToLower())
                {
                    SortOrder = SortOrder.Ascending;
                    SortedProperty = column.ColumnName;
                    column.SortIcon = CONST_ArrowDownClass;
                    column.SortExpression = column.ColumnName + "_desc";
                }

                if (sortExpression == column.ColumnName.ToLower() + "_desc")
                {
                    SortOrder = SortOrder.Descending;
                    SortedProperty = column.ColumnName;
                    column.SortIcon = CONST_ArrowUpClass;
                    column.SortExpression = column.ColumnName;
                }
            }
        }
    }
}
