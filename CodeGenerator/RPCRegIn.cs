namespace CodeGenerator
{
    public static class RPCRegIn
    {
        public static string Create(int cc)
        {
            string StackA0 = "";
            string StackA3 = "";
            string StackA7 = "";

            string a0 = "\n        /// <typeparam name=\"T~\">Тип аргумента №~</typeparam>".ToFormater();
            string a1 = "T1";
            string a3 = "\n        where T~ : unmanaged".ToFormater();
            string a5 = "IntPtr";
            string a6 = "IntPtr A1";
            string a7 =
                ("\n                T~* t~ = (T~*)(A~ + sizeof(BitArgument));" +
                "\n                args[~] = *t~;").ToFormater();



            string FormateringText = Code.ToOneString().ToFormater();




            string txt = "";
            for (int i = 0; i < cc; i++)
            {
                int i1 = i + 1;
                StackA0 += string.Format(a0, i1, i1).UndoFormater();
                StackA3 += string.Format(a3, i1).UndoFormater();
                StackA7 += string.Format(a7, i1, i1, i1, i1, i, i1).UndoFormater();

                txt += string.Format(FormateringText, i1, cc, StackA0, a1, a1, StackA3, i1, a5, a6, StackA7);
                a1 += string.Format(", T{0}", i + 2);
                a5 += ", IntPtr";
                a6 += string.Format(", IntPtr A{0}", i + 2);
            }
            txt = txt.UndoFormater();
            return txt;
        }

        static readonly string[] Code = new string[]
        {
        "        //Данная функция была сгенерирована автоматически в коде - CodeGenerator.RPCRegIn    [made by Master™]                  ",
        "        //Это перегрузка метода №~ из №~                                                                                        ",
        "                                                                                                                                ",
        "        /// <summary>                                                                                                           ",
        "        /// Регистрирует метод c аргументами для последующего вызова по сети                                                    ",
        "        /// </summary>~                                                                                                         ",
        "        /// <param name=\"Address\">Адресс функции</param>                                                                      ",
        "        /// <param name=\"LVL_Permission\">Уровень прав которыми должен обладать пользователь чтобы запустить</param>           ",
        "        /// <param name=\"Funct\">Функция</param>                                                                                ",
        "        /// <returns>Ссылка на функцию</returns>                                                                                ",
        "        public unsafe Handle RegisterMethod<~>(ushort Address, byte LVL_Permission, RPCfunct<~> Funct)~                         ",
        "        {                                                                                                                       ",
        "            MethodInfo mi = Funct.GetMethodInfo();                                                                              ",
        "            //массив аргументов                                                                                                 ",
        "            object[] args = new object[~];                                                                                      ",
        "            //Генерируем микрокод для распаковки массива байт в аргументы и запуска самой функции                               ",
        "            RPCfunct<~> act = (~) =>                                                                                            ",
        "            {~                                                                                                                  ",
        "                mi.Invoke(Funct.Target, args);                                                                                  ",
        "            };                                                                                                                  ",
        "                                                                                                                                ",
        "            MyAction action = new MyAction() { Context = act.Target, LVL_Permission = LVL_Permission, MethodInfo = act.Method };",
        "            //регистрирует функцию по указанному адресу                                                                         ",
        "            Functions[Address].Add(action);                                                                                     ",
        "            //создаёт  указатель на регистрацию для возможности дерегистрировать                                                ",
        "            Handle handle = new Handle(this, action, Address);                                                                  ",
        "                                                                                                                                ",
        "            return handle;                                                                                                      ",
        "        }                                                                                                                       ",
        "                                                                                                                                "
        };
    }
}
