﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using Silverback.Domain;
using Silverback.Tests.EventSourcing.TestTypes;
using Silverback.Tests.EventSourcing.TestTypes.EntityEvents;
using Xunit;

namespace Silverback.Tests.EventSourcing.Domain
{
    public class EventSourcingDomainEntityTests
    {
        [Fact]
        public void Constructor_PassingSomeEvents_EventsApplied()
        {
            var person = new Person(
                new IEntityEvent[]
                {
                    new NameChangedEvent { NewName = "Sergio" },
                    new AgeChangedEvent { NewAge = 35 }
                });

            person.Name.Should().Be("Sergio");
            person.Age.Should().Be(35);
        }

        [Fact]
        public void Constructor_PassingSomeEvents_EventsAppliedAccordingToTimestamp()
        {
            var person = new Person(
                new IEntityEvent[]
                {
                    new NameChangedEvent { NewName = "Sergio", Timestamp = new DateTime(2001, 01, 02) },
                    new NameChangedEvent { NewName = "Silverback", Timestamp = new DateTime(2001, 01, 01) }
                });

            person.Name.Should().Be("Sergio");
        }

        [Fact]
        public void Constructor_PassingSomeEvents_EventsAppliedAccordingToTimestampAndSequence()
        {
            var person = new Person(
                new IEntityEvent[]
                {
                    new NameChangedEvent
                        { NewName = "Sergio", Timestamp = new DateTime(2001, 01, 02), Sequence = 2 },
                    new NameChangedEvent
                        { NewName = "Mario", Timestamp = new DateTime(2001, 01, 02), Sequence = 1 },
                    new NameChangedEvent
                        { NewName = "Silverback", Timestamp = new DateTime(2001, 01, 01), Sequence = 2 }
                });

            person.Name.Should().Be("Sergio");
        }

        [Fact]
        public void GetNewEvents_WithNewAndOldEvents_OnlyNewEventsReturned()
        {
            var person = new Person(
                new IEntityEvent[]
                {
                    new NameChangedEvent { NewName = "Sergio" },
                    new AgeChangedEvent { NewAge = 35 }
                });

            person.ChangePhoneNumber("123456");

            person.GetNewEvents().Should().HaveCount(1);
        }

        [Fact]
        public void Constructor_PassingAnEvent_IsReplayingCorrectlySetToTrue()
        {
            var person = new Person(
                new IEntityEvent[]
                {
                    new PhoneNumberChangedEvent { NewPhoneNumber = "123456" }
                });

            person.PhoneNumber.Should().Be("123456*");
        }

        [Fact]
        public void AddAndApplyEvent_WhateverEvent_IsReplayingCorrectlySetToFalse()
        {
            var person = new Person();

            person.ChangePhoneNumber("123456");

            person.PhoneNumber.Should().Be("123456");
        }

        [Fact]
        public void AddAndApplyEvent_SomeEvents_EventsTimestampIsSet()
        {
            var now = DateTime.UtcNow;
            var person = new Person();

            person.ChangePhoneNumber("1");
            person.ChangePhoneNumber("2");
            person.ChangePhoneNumber("3");

            person.GetNewEvents().Should().HaveCount(3);
            person.GetNewEvents().Select(entityEvent => entityEvent.Timestamp).ToList()
                .ForEach(timestamp => timestamp.Should().BeAfter(now));
        }

        [Fact]
        public void AddAndApplyEvent_SomeEvents_EventsSequenceIsSet()
        {
            var person = new Person();

            person.ChangePhoneNumber("1");
            person.ChangePhoneNumber("2");
            person.ChangePhoneNumber("3");

            person.GetNewEvents().Select(entityEvent => entityEvent.Sequence)
                .Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void AddAndApplyEvent_EventsFromThePast_EventsTimestampIsPreserved()
        {
            var now = DateTime.UtcNow;
            var person = new Person();

            person.MergeEvents(
                new IEntityEvent[]
                {
                    new PhoneNumberChangedEvent
                        { NewPhoneNumber = "1", Timestamp = DateTime.Now.AddDays(-3) },
                    new PhoneNumberChangedEvent
                        { NewPhoneNumber = "2", Timestamp = DateTime.Now.AddDays(-2) },
                    new PhoneNumberChangedEvent { NewPhoneNumber = "3", Timestamp = DateTime.Now.AddDays(-1) }
                });

            person.GetNewEvents().Should().HaveCount(3);
            person.GetNewEvents().Select(entityEvent => entityEvent.Timestamp).ToList()
                .ForEach(timestamp => timestamp.Should().BeBefore(now));
        }

        [Fact]
        public void AddAndApplyEvent_EventsFromThePast_EventsSequenceIsPreserved()
        {
            var person = new Person();

            person.MergeEvents(
                new IEntityEvent[]
                {
                    new PhoneNumberChangedEvent { NewPhoneNumber = "1", Sequence = 100 },
                    new PhoneNumberChangedEvent { NewPhoneNumber = "2", Sequence = 101 },
                    new PhoneNumberChangedEvent { NewPhoneNumber = "3", Sequence = 102 }
                });

            person.GetNewEvents().Select(entityEvent => entityEvent.Sequence)
                .Should().BeEquivalentTo(new[] { 100, 101, 102 });
        }

        [Fact]
        public void AddAndApplyEvent_MergingEventsFromThePast_CorrectSequenceIsRecognizable()
        {
            var person = new Person(
                new IEntityEvent[]
                {
                    new NameChangedEvent { NewName = "1", Timestamp = DateTime.Today.AddDays(-10) },
                    new NameChangedEvent { NewName = "2", Timestamp = DateTime.Today.AddDays(-8) },
                    new NameChangedEvent { NewName = "3", Timestamp = DateTime.Today.AddDays(-5) }
                });

            person.MergeEvents(
                new IEntityEvent[]
                {
                    new NameChangedEvent { NewName = "4", Timestamp = DateTime.Today.AddDays(-9) },
                    new NameChangedEvent { NewName = "5", Timestamp = DateTime.Today.AddDays(-7) },
                    new NameChangedEvent { NewName = "6", Timestamp = DateTime.Today.AddDays(-6) }
                });

            person.Name.Should().Be("3");
        }

        [Fact]
        public void AddAndApplyEvent_MergingEventsFromThePast_ConcurrencyResolvedConsistently()
        {
            var person = new Person(
                new IEntityEvent[]
                {
                    new NameChangedEvent { NewName = "1", Timestamp = DateTime.Today.AddDays(-10) },
                    new NameChangedEvent { NewName = "2", Timestamp = DateTime.Today.AddDays(-5) },
                    new NameChangedEvent { NewName = "3", Timestamp = DateTime.Today.AddDays(-5) },
                    new NameChangedEvent { NewName = "4", Timestamp = DateTime.Today.AddDays(-9) }
                });

            person.MergeEvents(
                new IEntityEvent[]
                {
                    new NameChangedEvent { NewName = "5", Timestamp = DateTime.Today.AddDays(-5) },
                    new NameChangedEvent { NewName = "6", Timestamp = DateTime.Today.AddDays(-5) },
                    new NameChangedEvent { NewName = "7", Timestamp = DateTime.Today.AddDays(-7) }
                });

            person.Name.Should().Be("6");

            var person2 = new Person(person.Events.ToList());

            person2.Name.Should().Be("6");
        }

        [Fact]
        public void AddAndApplyEvent_SomeEventsAppendedToOldEvents_EventsSequenceIsSet()
        {
            var person = new Person(
                new IEntityEvent[]
                {
                    new NameChangedEvent { NewName = "Sergio" },
                    new PhoneNumberChangedEvent { NewPhoneNumber = "123456" }
                });

            person.ChangePhoneNumber("3");
            person.ChangePhoneNumber("4");
            person.ChangePhoneNumber("5");

            person.GetNewEvents().Select(entityEvent => entityEvent.Sequence)
                .Should().BeEquivalentTo(new[] { 3, 4, 5 });
        }

        [Fact]
        public void GetNewEvents_SomeNewEventsApplied_NewEventsReturned()
        {
            var person = new Person();

            person.ChangePhoneNumber("1");
            person.ChangePhoneNumber("2");
            person.ChangePhoneNumber("3");

            person.GetNewEvents().Should().HaveCount(3);
        }

        [Fact]
        public void GetNewEvents_SomeNewEventsApplied_OnlyNewEventsReturned()
        {
            var person = new Person(
                new IEntityEvent[]
                {
                    new NameChangedEvent { NewName = "Sergio" },
                    new PhoneNumberChangedEvent { NewPhoneNumber = "123456" }
                });

            person.ChangePhoneNumber("1");
            person.ChangePhoneNumber("2");
            person.ChangePhoneNumber("3");

            person.GetNewEvents().Should().HaveCount(3);
        }

        [Fact]
        public void GetNewEvents_SomeNewEventsFromThePastApplied_NewEventsReturned()
        {
            var person = new Person(
                new IEntityEvent[]
                {
                    new NameChangedEvent { NewName = "Sergio" },
                    new PhoneNumberChangedEvent { NewPhoneNumber = "123456" }
                });

            person.MergeEvents(
                new IEntityEvent[]
                {
                    new PhoneNumberChangedEvent
                        { NewPhoneNumber = "1", Timestamp = DateTime.Now.AddDays(-3) },
                    new PhoneNumberChangedEvent
                        { NewPhoneNumber = "2", Timestamp = DateTime.Now.AddDays(-2) },
                    new PhoneNumberChangedEvent { NewPhoneNumber = "3", Timestamp = DateTime.Now.AddDays(-1) }
                });

            person.GetNewEvents().Should().HaveCount(3);
        }
    }
}
