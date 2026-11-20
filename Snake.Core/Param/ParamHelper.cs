using System.Text;

namespace Snake.Core
{
    public static class ParamHelper
    {
        public static readonly string CurrentFilter = "CurrentFilter";
        public static readonly string SortModel = "sortModel";
        public static readonly string FromDateTicks = "FromDateTicks";
        public static readonly string ToDateTicks = "ToDateTicks";
        public static readonly string CurrentPage = "CurrentPage";
        public static readonly string PageSize = "PageSize";
        public static readonly string CN = "CN";
        public static readonly string DataSeparator = "#";
        public static readonly string CertificateId = "CertificateId";
        public static readonly string CrlId = "CrlId";
        public static readonly int MaxGetRecords = 100;

        public static string CombiningString(params string[] list)
        {
            var sp = new StringBuilder();

            foreach (var item in list)
            {
                sp.Append(item);
            }

            return sp.ToString();
        }

        public static string CombiningStringWithSepararor(params string[] list)
        {
            var sp = new StringBuilder();

            for (int i = 0; i < list.Length; i++)
            {
                if (i > 0)
                {
                    sp.Append(DataSeparator);
                }

                sp.Append(list[i]);
            }

            return sp.ToString();
        }
    }
}