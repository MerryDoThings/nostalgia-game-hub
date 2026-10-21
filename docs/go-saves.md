# Copie locali dei salvataggi di Angry Birds Go!

La cartella sorgente `scripts/go-backup` contiene uno strumento facoltativo per
Go 1.8.x su BlueStacks 5. Richiede Python 3.10 o successivo e le dipendenze nel
file `requirements.txt`. Non fa parte dello ZIP del solo launcher.

Da PowerShell nella cartella del repository:

```powershell
py -m pip install -r scripts/go-backup/requirements.txt
py scripts/go-backup/backup.py --once
```

Il secondo comando copia i salvataggi validi presenti nell'istanza `Pie64` in
`%LOCALAPPDATA%\NostalgiaGameHub\Backups\Go`. Con `--instance` puoi scegliere
un'altra istanza; `--disk` e `--backup-dir` consentono di scegliere i percorsi.
`--once` funziona anche con una copia del disco dell'emulatore chiuso.

Per avviare Go e conservare nuove copie durante la sessione:

```powershell
py scripts/go-backup/backup.py --launch
```

Lo strumento legge il disco ogni 15 secondi, verifica lunghezza, checksum del
file, decompressione e checksum del contenuto prima di copiarlo. Conserva le
versioni precedenti e ignora le letture incomplete durante una scrittura del
gioco. Quando BlueStacks è chiuso, termina dopo circa un minuto. Non richiede
ADB, root o cambiamenti alle impostazioni dell'emulatore.

Il disco di BlueStacks viene aperto soltanto in lettura. Non vengono cancellati
file, installati APK o ripristinati automaticamente salvataggi. Le copie sono
locali e possono contenere il tuo profilo: mantienile fuori dai repository pubblici.

## Se Go torna al tutorial

Conserva prima una copia del disco dell'emulatore chiuso e delle eventuali copie
dei salvataggi. Evita di disinstallare o cancellare i dati mentre cerchi il recupero.
Una copia con checksum corretto è utile per il recupero, ma non garantisce che
ogni variante del gioco ne accetti il contenuto.

Un problema verificato sulla versione 1.8.7 riguarda alcune modifiche alle gemme:
il codice che aggiunge valuta può arrivare a 999.999.999, mentre il caricamento
accetta al massimo 800.235.657. Un saldo superiore può quindi far rifiutare un
salvataggio integro alla riapertura. Occorre correggere la variante del gioco e
poi ripristinare una copia valida. Questo launcher non modifica il gioco.

## Avvio senza Internet

Il blocco dopo il logo Rovio e prima dei pulsanti del menu richiede una verifica
separata. L'analisi statica della versione 1.8.7 mostra attese per la sessione
Skynest e per i metadati degli aggiornamenti: alcune chiamate consentono fino a
60 secondi di attesa. Il controllo della sessione può terminare prima quando
Android segnala la disconnessione.

Queste attese sono una pista da verificare durante un avvio offline. Non è
ancora confermato quale passaggio impedisca di arrivare al menu. Lo strumento
di copia dei salvataggi non corregge questo comportamento.
