﻿using Dapplo.Config.Tests.ConfigBaseTests.Interfaces;

namespace Dapplo.Config.Tests.ConfigBaseTests.Entities
{
    public class BassicAssignImpl : ConfigurationBase<IBassicAssignTest>, IBassicAssignTest
    {
        public string Name { get; set; }
    }
}
