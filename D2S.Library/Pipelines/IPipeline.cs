using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Pipelines
{
    public interface IPipeline
    {
        /// <summary>
        /// Event that fires when when a certain amount of lines has been read from the file. The amount is equal to 1,000 multiplied by the LineReadIntervalMultiplier property
        /// </summary>
        event EventHandler<int> LinesReadFromFile;
        /// <summary>
        /// Starts the execution of the Pipeline asyncronously
        /// </summary>
        /// <returns>A task object representing the state of the pipeline</returns>
        Task StartAsync();
        /// <summary>
        /// Toggles the paused state of the pipeline, pausing it if it is running and unpausing otherwise. Returns true if the pause was toggled or false if the pipeline does not support pausing.
        /// </summary>
        /// <returns></returns>
        bool TogglePause();
        /// <summary>
        /// Returns true if pipeline is in the paused state and false if otherwise.
        /// </summary>
        bool IsPaused { get; }
    }
}
