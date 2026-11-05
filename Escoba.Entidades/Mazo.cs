using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Escoba.Entidades
{
    public class Mazo
    {
        [Key]
        public int Id { get; set; }

        // Colección de cartas disponibles. Las cartas repartidas se quitan de esta lista.
        public List<Carta> Cartas { get; set; } = new List<Carta>();

        public Mazo()
        {
            // Cada mazo nuevo comienza con las 40 cartas de la baraja española.
            InicializarMazoEspanol();
        }

        public void InicializarMazoEspanol()
        {
            // Se limpia el mazo para permitir reiniciar una partida sin duplicar cartas.
            Cartas.Clear();
            string[] palos = { "Oros", "Copas", "Espadas", "Bastos" };
            int[] numeros = { 1, 2, 3, 4, 5, 6, 7, 10, 11, 12 };

            var siguienteId = 1;
            foreach (var palo in palos)
            {
                foreach (var num in numeros)
                {
                    // Cada combinación de palo y número representa una carta única.
                    Cartas.Add(new Carta
                    {
                        // Las cartas de una partida viven solo en memoria: EF no les
                        // asigna un Id. El identificador debe ser único y estable.
                        Id = siguienteId++,
                        Palo = palo,
                        Numero = num
                    });
                }
            }
        }

        public void MezclarCartas()
        {
            // Reordena las cartas antes de repartirlas para que la partida sea aleatoria.
            Random rng = new Random();
            Cartas = Cartas.OrderBy(_ => rng.Next()).ToList();
        }

        public List<Carta> RepartirCartas(int cantidad)
        {
            var cartasRepartidas = new List<Carta>();

            for (int i = 0; i < cantidad; i++)
            {
                if (Cartas.Count > 0)
                {
                    // Siempre se toma la primera carta y se elimina del mazo para no repartirla dos veces.
                    var carta = Cartas[0];
                    Cartas.RemoveAt(0);
                    cartasRepartidas.Add(carta);
                }
            }

            return cartasRepartidas;
        }
    }
}
