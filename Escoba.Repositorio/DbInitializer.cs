using System;
using System.Collections.Generic;
using System.Text;
using Escoba.Entidades;
using Microsoft.AspNetCore.Identity;

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
            var passwordHasher = new PasswordHasher<Usuario>();

            var user1 = new Usuario { Nombre = "admin" };
            user1.Password = passwordHasher.HashPassword(user1, "1234");
            var user2 = new Usuario { Nombre = "jugador" };
            user2.Password = passwordHasher.HashPassword(user2, "1234");
            var user3 = new Usuario { Nombre = "juan" };
            user3.Password = passwordHasher.HashPassword(user3, "1234");
            var usuarios = new Usuario[]
            {
                user1,
                user2,
                user3
            };

            context.Usuarios.AddRange(usuarios);
            context.SaveChanges();
        }
    }
}
