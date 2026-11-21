namespace Loto.Core
{
    // Spanish texts shown to the student for each error code.
    public static class ErrorTexts
    {
        public static string Title(string code)
        {
            switch (code)
            {
                case Referee.TouchBeforeOff: return "Tocaste la máquina encendida";
                case Referee.TouchWithoutLock: return "Tocaste la máquina sin candado";
                case Referee.TouchWithoutVerify: return "Tocaste la máquina sin verificar";
                case Referee.LockBeforeOff: return "Primero apaga la máquina";
                case Referee.VerifyWithoutLock: return "Falta el candado";
                case Referee.LeverBlocked: return "La palanca está bloqueada";
                case null:
                case "": return "";
                default: return "Paso incorrecto";
            }
        }

        public static string Explain(string code)
        {
            switch (code)
            {
                case Referee.TouchBeforeOff:
                    return "Metiste la mano sin bajar la palanca: la máquina seguía con energía. " +
                           "En un taller real podías perder la mano o recibir una descarga.";
                case Referee.TouchWithoutLock:
                    return "Apagaste la máquina pero no pusiste tu candado. " +
                           "En un taller real un compañero puede volver a encenderla mientras tienes las manos adentro.";
                case Referee.TouchWithoutVerify:
                    return "Pusiste el candado pero no verificaste que la máquina quedó sin energía. " +
                           "En un taller real puede quedar energía guardada y la máquina arranca igual.";
                case Referee.LockBeforeOff:
                    return "Quisiste poner el candado con la palanca todavía encendida. " +
                           "El candado solo protege si bloquea la palanca en apagado: primero baja la palanca.";
                case Referee.VerifyWithoutLock:
                    return "Apretaste verificar sin tener el candado puesto. " +
                           "Sin candado cualquiera puede encender la máquina después de tu prueba: primero pon el candado.";
                case Referee.LeverBlocked:
                    return "La palanca no sube porque tiene un candado puesto. " +
                           "Para eso sirve el bloqueo: nadie puede encender la máquina hasta que el dueño del candado lo retire.";
                case null:
                case "":
                    return "";
                default:
                    return "Ese paso no va en este momento. Revisa el orden: apagar, candado, verificar y recién tocar.";
            }
        }
    }
}
