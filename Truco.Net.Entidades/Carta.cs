using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Truco.Net.Entidades
{
    public class Carta
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El palo de la carta es obligatorio.")]
        [StringLength(20, ErrorMessage = "El palo no puede superar los 20 caracteres.")]
        public string Palo { get; set; } = string.Empty; // Oros, Copas, Espadas, Bastos

        [Required(ErrorMessage = "El número de la carta es obligatorio.")]
        [Range(1, 12, ErrorMessage = "El número de la carta debe estar entre 1 y 12.")]
        public int Numero { get; set; } // 1 a 7, 10 (Sota), 11 (Caballo), 12 (Rey)

        // Relación opcional 
        public int? MazoId { get; set; }

        [ForeignKey("MazoId")]
        public Mazo? MazoAsociado { get; set; }



        public bool EsSieteDeVelos()
        {
            return Palo.Equals("Oro", StringComparison.OrdinalIgnoreCase) && Numero == 7;
        }
    }
}
