using System;
using System.Collections.Generic;
using System.Text;
using Escoba.Entidades;

namespace Escoba.Repositorio
{
    public class DbInitializer
    {
        public static void Initialize(EscobaDbContext context)
        {
            // Asegura que la base de datos esté creada
            context.Database.EnsureCreated();

            // Si ya existen usuarios, no hace nada para evitar duplicados
            if (context.Usuarios.Any())
            {
                return;
            }

            // Definimos los 3 usuarios iniciales
            var usuarios = new Usuario[]
            {
                new Usuario { Nombre = "admin", Password = "12345" },
                new Usuario { Nombre = "jugador1", Password = "truco2026" },
                new Usuario { Nombre = "juan", Password = "secret" }
            };

            context.Usuarios.AddRange(usuarios);
            context.SaveChanges();
        }
    }
}
