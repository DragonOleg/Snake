using System;
using System.ComponentModel;
using System.Reflection;

namespace Snake.Core
{
    public static class EnumHelper
    {
        public static T ToEnum<T>(this string enumString)
        {
            return (T)Enum.Parse(typeof(T), enumString);
        }

        public static T ToEnum<T>(this int enumInt)
        {
            return (T)Enum.ToObject(typeof(T), enumInt);
        }

        public static T ToEnum<T>(this int? enumNullInt)
        {
            if (!enumNullInt.HasValue)
            {
                return default(T);
            }

            return enumNullInt.Value.ToEnum<T>();
        }

        public static T ToEnum<T>(this long enumLong)
        {
            return (T)Enum.ToObject(typeof(T), enumLong);
        }

        public static T ToEnum<T>(this long? enumNullLong)
        {
            if (!enumNullLong.HasValue)
            {
                return default(T);
            }

            return enumNullLong.Value.ToEnum<T>();
        }

        public static string GetDescription(this Enum enumElement)
        {
            Type type = enumElement.GetType();

            MemberInfo[] memInfo = type.GetMember(enumElement.ToString());
            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs != null && attrs.Length > 0)
                    return ((DescriptionAttribute)attrs[0]).Description;
            }

            return enumElement.ToString();
        }
    }
}
