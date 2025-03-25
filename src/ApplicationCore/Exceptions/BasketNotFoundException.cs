using System;

namespace Microsoft.eShopWeb.ApplicationCore.Exceptions
{
    public class BasketNotFoundException : Exception
    {
        public BasketNotFoundException(int basketId) : base($"No basket found with id {basketId}")
        {
        }

        public BasketNotFoundException(string message) : base(message)
        {
        }

        public BasketNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
