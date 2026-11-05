using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Escoba.Entidades
{
    public class Carta
    {
        // Identificador utilizado por Entity Framework cuando la carta se persiste.
        [Key]
        public int Id { get; set; }

        // Palo de la baraja española: Oros, Copas, Espadas o Bastos.
        [Required(ErrorMessage = "El palo de la carta es obligatorio.")]
        [StringLength(20, ErrorMessage = "El palo no puede superar los 20 caracteres.")]
        public string Palo { get; set; } = string.Empty; // Oros, Copas, Espadas, Bastos

        // La baraja de 40 cartas utiliza 1 a 7, 10 (Sota), 11 (Caballo) y 12 (Rey).
        [Required(ErrorMessage = "El número de la carta es obligatorio.")]
        [Range(1, 12, ErrorMessage = "El número de la carta debe estar entre 1 y 12.")]
        public int Numero { get; set; } // 1 a 7, 10 (Sota), 11 (Caballo), 12 (Rey)

        // Valor que se utiliza para calcular las sumas de 15 en el juego de Escoba.
        [NotMapped]
        public int ValorEscoba => Numero switch
        {
            1 => 1,
            >= 2 and <= 7 => Numero,
            10 => 8,
            11 => 9,
            12 => 10,
            _ => throw new InvalidOperationException($"El número {Numero} no es válido para una carta de Escoba.")
        };

        // Ruta pública del archivo de imagen asociado a esta carta.
        [NotMapped]
        public string RutaImagen => $"/Assets/cartas/{Numero}{ObtenerPaloParaArchivo(Palo)}.svg";

        // Texto descriptivo utilizado en la interfaz y en el atributo alt de la imagen.
        [NotMapped]
        public string Nombre => $"{Numero} de {Palo}";

        // Relación opcional entre la carta y el mazo al que pertenece.
        public int? MazoId { get; set; }

        [ForeignKey("MazoId")]
        public Mazo? MazoAsociado { get; set; }


        // Indica si la carta es el 7 de Oros, que otorga un punto especial.
        public bool EsSieteDeVelos()
        {
            return (Palo.Equals("Oro", StringComparison.OrdinalIgnoreCase) ||
                    Palo.Equals("Oros", StringComparison.OrdinalIgnoreCase)) && Numero == 7;
        }

        private static string ObtenerPaloParaArchivo(string palo)
        {
            // El modelo usa nombres plurales, pero los archivos usan nombres singulares.
            return palo switch
            {
                "Oros" => "Oro",
                "Copas" => "Copa",
                "Espadas" => "Espada",
                "Bastos" => "Basto",
                _ => palo
            };
        }
    }
}
