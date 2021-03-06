﻿using System;
using System.Collections.Generic;
using REstate.Schematics;

namespace REstate.Engine.Repositories
{
    public class MachineStatus<TState, TInput>
    {
        public string MachineId { get; set; }

        public Schematic<TState, TInput> Schematic { get; set; }

        public TState State { get; set; }

        public long CommitNumber { get; set; }

        public DateTimeOffset UpdatedTime { get; set; }

        public IDictionary<string, string> Metadata { get; set; }

        public IDictionary<string, string> StateBag { get; set; }

        public static implicit operator Status<TState>(MachineStatus<TState, TInput> record)
        {
            return new Status<TState>(record.MachineId, record.State, record.UpdatedTime, record.CommitNumber, record.StateBag);
        }
    }
}
