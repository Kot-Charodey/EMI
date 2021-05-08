using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace EMI
{
    /// <summary>
    /// указывает что этот метод будет автоматически зарегистрирован
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcMethod : System.Attribute
	{
        /// <summary>
        /// Адресс вызываймого метода
        /// </summary>
    	public ushort Address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address">Адресс вызываймого метода</param>
        public RpcMethod(ushort address)
   		{
        	Address=address;
    	}
	} 
}