using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameServerManagerWebApp.Entites
{
    public class GameServerConfiguration
    {
        public int GameServerConfigurationID { get; set; }

        public bool IsActive { get; set; }

        public string AccessToken { get; set; }

        [Display(Name = "Serveur")]
        public int GameServerID { get; set; }
        [Display(Name = "Serveur")]
        public GameServer GameServer { get; set; }

        [Display(Name = "Nom du serveur (visible en jeu)")]
        public string ServerName { get; set; }
        [Display(Name = "Mot de passe")]
        public string ServerPassword { get; set; }
        [Display(Name = "Fichier de mission")]
        public string ServerMission { get; set; }

        [Display(Name = "Serveur TeamSpeak")]
        public string VoipServer { get; set; }
        [Display(Name = "Canal TeamSpeak")]
        public string VoipChannel { get; set; }
        [Display(Name = "Mot de passe TeamSpeak")]
        public string VoipPassword { get; set; }

        [Display(Name = "Lien vers la page associée (événement, site officiel ou autre)")]
        public string EventHref { get; set; }
        [Display(Name = "Image de fond")]
        public string EventImage { get; set; }

        [Display(Name = "Modpack")]
        public int? ModsetID { get; set; }
        [Display(Name = "Modpack")]
        public Modset Modset { get; set; }

        public List<GameConfigurationFile> Files { get; set; }
        [Display(Name = "Libellé")]
        public string Label { get; set; }
        public DateTime LastChangeUTC { get; set; }

        [Display(Name = "Nom de la sauvegarde (Mod 1erGTD)")]
        public string GamePersistName { get; set; }
    }
}
