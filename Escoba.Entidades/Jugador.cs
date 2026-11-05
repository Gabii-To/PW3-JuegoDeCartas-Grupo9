using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Escoba.Entidades;

namespace Escoba.Entidades
{
    public class Jugador
    {
        [Key]
        public int Id { get; set; }


        [Required(ErrorMessage = "El jugador debe estar asociado a un usuario.")]
        public int UsuarioId { get; set; }

        [NotMapped]
        public string ConnectionId { get; set; }


        [ForeignKey("UsuarioId")]
        public Usuario UsuarioAsociado { get; set; } = null!;

        // Relación con la Partida 
        public int? PartidaId { get; set; }

        [ForeignKey("PartidaId")]
        public Partida? Partida { get; set; }

        [NotMapped]
        public List<Carta> CartasEnMano { get; set; } = new List<Carta>();
        [NotMapped]
        public List<Carta> CartasGanadas { get; set; } = new List<Carta>();

        [NotMapped]
        public int Escobas { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El puntaje no puede ser negativo.")]
        public int Puntos { get; set; } = 0;
        [NotMapped]
        public bool EsTurnoActual { get; set; } = false;
    }
}
