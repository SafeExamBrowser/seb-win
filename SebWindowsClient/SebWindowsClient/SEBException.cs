using System;

// -------------------------------------------------------------
//     Viktor Tomas
//     BFH-TI, http://www.ti.bfh.ch
//     Biel, 2012
// -------------------------------------------------------------

namespace SebWindowsClient
{
    /// ---------------------------------------------------------------------------------------
    /// <summary>
    /// This exception is thrown when an exception specific to the SEB occurs.
    /// </summary>
    /// ---------------------------------------------------------------------------------------
    public class SEBException : ApplicationException
    {
        /// ---------------------------------------------------------------------------------------
        /// <summary>
        /// Initialize a new instance of the SEBException exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// ---------------------------------------------------------------------------------------
        public SEBException(string message, Exception innerException = null)
            : base(message, innerException)
        {

        }

        /// ---------------------------------------------------------------------------------------
        /// <summary>
        /// Get or set the source of the exception.
        /// </summary>
        /// ---------------------------------------------------------------------------------------
        public override string Source
        {
            get { return "SEB"; }
            set { base.Source = value; }
        }
    }

    public class SEBNotAllowedToRunEception : Exception
    {
        public SEBNotAllowedToRunEception(string message) : base(message)
        {
        }
    }
}