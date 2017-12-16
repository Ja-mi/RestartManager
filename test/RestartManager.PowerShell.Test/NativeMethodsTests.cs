﻿// <copyright file="NativeMethodsTests.cs" company="Heath Stewart">
// Copyright (c) 2017 Heath Stewart
// See the LICENSE.txt file in the project root for more information.
// </copyright>

namespace RestartManager
{
    using System;
    using Moq;
    using Xunit;

    public class NativeMethodsTests
    {
        [Fact]
        public void RM_UNIQUE_PROCESS_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>("process", () => new RM_UNIQUE_PROCESS(null));
        }

        [Fact]
        public void RM_UNIQUE_PROCESS()
        {
            var process = Mock.Of<IProcess>(x => x.Id == 1234 && x.StartTime == new DateTime(2017, 11, 25, 17, 55, 00, 00, DateTimeKind.Local));

            var sut = new RM_UNIQUE_PROCESS(process);
            Assert.Equal(1234, sut.dwProcessId);
            Assert.Equal(30631513, sut.ProcessStartTime.dwHighDateTime);
            Assert.Equal(-1856966144, sut.ProcessStartTime.dwLowDateTime);
        }
    }
}
