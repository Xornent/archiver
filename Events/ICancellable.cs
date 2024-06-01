using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archiver.Events
{
    /// <summary>
    /// The definition of the interface which supports the cancellation of a process.
    /// </summary>
    public interface ICancellable
    {
        /// <summary>
        /// Gets or sets whether to stop the current archive operation.
        /// </summary>
        bool Cancel { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the current file.
        /// </summary>
        bool Skip { get; set; }
    }
}
