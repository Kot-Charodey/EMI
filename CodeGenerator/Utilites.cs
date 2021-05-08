using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator
{
    public static class Utilites
    {
        public static string ToOneString(this string[] array)
        {
            string txt = "";
            for (int i = 0; i < array.Length; i++)
            {
                txt += array[i] + "\n";
            }
            return txt;
        }

        public static string ToFormater(this string txt)
        {
            return txt.ChangeParentheses().TildaToFormat();
        }
        public static string UndoFormater(this string txt)
        {
            return txt.UndoChangeParentheses();
        }

        public static string TildaToFormat(this string txt)
        {
            int index;
            int argN = 0;
            while ((index = txt.IndexOf('~')) > -1)
            {
                txt = txt.Remove(index, 1).Insert(index, "{" + argN + "}");
                argN++;
            }
            return txt;
        }

        private static string ChangeParentheses(this string txt)
        {
            return txt.Replace('{', '⌈').Replace('}', '⌉');
        }

        private static string UndoChangeParentheses(this string txt)
        {
            return txt.Replace('⌈', '{').Replace('⌉', '}');
        }
    }
}
