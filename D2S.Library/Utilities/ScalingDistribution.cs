using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Utilities
{
    public class ScalingDistribution
    {
        public int[] TaskDistribution { get; private set; }
        private ScalingDistribution(int numSeq, int[] ratio, int cpu)
        {
            //read task is always one task as we can't read from a file with more than one thread/
            //remaining tasks are divided according to a set ratio applied over the number of available CPU's
            TaskDistribution = new int[numSeq];
            TaskDistribution[0] = 1;
            double cpuAvailable = cpu - 1; //minus one which is used to handle reading            
            double sumRatio = ratio.Sum() - 1; //subtract the 1 read tasks from the ratio
            if (cpuAvailable < (numSeq-1))
            {
                throw new ArgumentException($"The amount of logical cores available for tasks other than reading ({cpuAvailable})" +
                    $" must be at least equal to the number of distinct non-reading tasks ({sumRatio}) to be able to construct a ScalingDistribution");
            }
            double cpuPerTask = cpuAvailable / sumRatio;

            //fill out the remainder of the distribution
            for (int i = 1; i <numSeq; i++)
            {
                double dRatio = ratio[i];
                double resultNumber = Math.Round(dRatio * cpuPerTask);
                TaskDistribution[i] = (int)resultNumber;

            }
        }
        /// <summary>
        /// Gets a <see cref="ScalingDistribution"/> object tailored to the specified number of discrete tasks and number of Cpu's
        /// </summary>
        /// <param name="numberOfSequentialTasks"></param>
        /// <param name="numberOfCpuToConsider"></param>
        /// <returns></returns>
        public static ScalingDistribution GetScalingDistribution(int numberOfSequentialTasks, int numberOfCpuToConsider)
        {
            try
            {
                int[] ratio = ComputeRatio(numberOfSequentialTasks);
                return new ScalingDistribution(numberOfSequentialTasks, ratio, numberOfCpuToConsider);
            }
            catch (ArgumentOutOfRangeException)
            {

                throw new ArgumentOutOfRangeException("numberOfSequentialTasks", "This number of sequential tasks is not supported");
            }
            
        }
        /// <summary>
        /// Gets a <see cref="ScalingDistribution"/> object tailored to the specified number of discrete tasks, using auto-cpu count.
        /// </summary>
        /// <param name="numberOfSequentialTasks"></param>
        /// <param name="numberOfCpuToConsider"></param>
        /// <returns></returns>
        public static ScalingDistribution GetScalingDistribution(int numberOfSequentialTasks)
        {
            return GetScalingDistribution(numberOfSequentialTasks, Environment.ProcessorCount);
        }

        private static int[] ComputeRatio(int NumSeq)
        {
            switch (NumSeq)
            {
                case 3:
                    return new int[] { 1, 1, 2 };
                case 4:
                    return new int[] { 1, 2, 2, 4 };
                case 5:
                    return new int[] { 1, 2, 2, 2, 4 };
                default:
                    throw new ArgumentOutOfRangeException("NumSeq", "This number of sequential tasks is not supported");
            }
        }
    }
}
