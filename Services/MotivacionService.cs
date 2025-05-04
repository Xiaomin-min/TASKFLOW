using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskFlowApi.Services
{
    public class MotivacionService // Clase concreta
    {
        private static readonly List<string> _mensajes = new List<string>
        {
            "¡Excelente trabajo! Sigue así.",
            "¡Tarea completada! Eres imparable.",
            "¡Un paso más cerca de tus metas!",
            "¡Bien hecho! Cada tarea cuenta.",
            "¡Fantástico! Tu esfuerzo da frutos.",
            "¡Lo lograste! Tómate un respiro.",
            "¡Increíble! Sigue conquistando tus tareas."
        };

        private static readonly Random _random = new Random();

        // --- CORRECCIÓN: Añadir 'virtual' de nuevo ---
        public virtual string ObtenerMensajeAleatorio()
        {
            if (_mensajes == null || !_mensajes.Any())
            {
                return "¡Sigue adelante!";
            }
            int indice = _random.Next(_mensajes.Count);
            return _mensajes[indice];
        }
    }
}