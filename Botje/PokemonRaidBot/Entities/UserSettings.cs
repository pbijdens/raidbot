﻿using Botje.DB;
using Botje.Messaging.Models;
using System;

namespace PokemonRaidBot.Entities
{
    public class UserSettings : IAtom
    {
        public Guid UniqueID { get; set; }

        public User User { get; set; }

        public Team Team { get; set; }

        public string Alias { get; set; }

        public int Level { get; set; }
    }
}