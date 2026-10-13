using System;
using System.Collections.Generic;
using Truco.Net.Entidades;

namespace Truco.Net.Entidades
{
    public class Mazo
    {
        private List<Carta> cartas;

        public Mazo()
        {
            cartas = new List<Carta>();
            CrearMazo();
        }

        private void CrearMazo()
        {
            Palo[] palos = (Palo[])Enum.GetValues(typeof(Palo));
            int[] numeros = { 1, 2, 3, 4, 5, 6, 7, 10, 11, 12 };

            foreach (Palo palo in palos)
            {
                foreach (int numero in numeros)
                {
                    cartas.Add(new Carta(numero, palo));
                }
            }
        }

        public void Barajar()
        {
            Random random = new Random();

            for (int i = cartas.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                Carta auxiliar = cartas[i];
                cartas[i] = cartas[j];
                cartas[j] = auxiliar;
            }
        }

        public Carta RobarCarta()
        {
            if (cartas.Count == 0)
            {
                return null;
            }

            Carta carta = cartas[0];
            cartas.RemoveAt(0);

            return carta;
        }

        public int CantidadCartas()
        {
            return cartas.Count;
        }
    }
}