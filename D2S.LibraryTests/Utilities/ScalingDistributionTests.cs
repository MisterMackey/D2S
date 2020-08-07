using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2S.Library.Utilities;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Utilities.Tests
{
    [TestClass()]
    public class ScalingDistributionTests
    {
        [TestMethod()]
        public void GetScalingDistributionTest1()
        {
            int numCPU = Environment.ProcessorCount;

            for (int i = 3; i < 6; i++)
            {
                if (numCPU < i)
                {
                    try
                    {
                        var result = ScalingDistribution.GetScalingDistribution(i); //this should fail
                        Assert.Fail(); //we should not reach this line
                    }
                    catch (ArgumentException e)
                    {
                        Assert.IsTrue(true);
                    }
                }
                else
                {
                    var result = ScalingDistribution.GetScalingDistribution(i); //this should not fail
                    Assert.AreEqual(expected: i, actual:result.TaskDistribution.Count());
                }
                
            }
        }
        [TestMethod()]
        public void GetScalingDistributionTest2()
        {
            

            for (int i = 3; i < 6; i++)
            {
                for (int numCPU = 0; numCPU < 33; numCPU++)
                {
                    if (numCPU < i)
                    {
                        try
                        {
                            var result = ScalingDistribution.GetScalingDistribution(i, numCPU); //this should fail
                            Assert.Fail(); //we should not reach this line
                        }
                        catch (ArgumentException e)
                        {
                            Assert.IsTrue(true);
                        }
                    }
                    else
                    {
                        var result = ScalingDistribution.GetScalingDistribution(i, numCPU); //this should not fail
                        Assert.AreEqual(expected: i, actual: result.TaskDistribution.Count());
                    } 
                }

            }
        }

    }
}