namespace Sfsp
{
    public enum TransferStatus
    {
        /// <summary>
        /// L'oggetto rappresentante il trasferimento è appena stato inizializzato e i due host non hanno ancora comunicato tra l'oro
        /// </summary>
        New = 0,

        /// <summary>
        /// L'host che ha ricevuto la richiesta di trasferimento (locale o remoto) deve accettarla
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Il trasferimento è in corso
        /// </summary>
        InProgress = 2,

        /// <summary>
        /// Il trasferimento è completato con successo
        /// </summary>
        Completed = 3,

        /// <summary>
        /// Il trasferimento è terminato con un errore o è stato interrotto prima del termine
        /// </summary>
        Failed = 4
    }
}
