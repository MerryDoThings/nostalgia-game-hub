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

La versione 1.8.7 può restare a lungo dopo il logo Rovio e prima dei pulsanti
del menu mentre aspetta i metadati Skynest. In una prova con la modalità aereo
di BlueStacks il menu era visibile dopo circa 80 secondi dall'avvio del gioco.

È stata verificata una modifica locale a `DownloadRemoteAssetList`: il ciclo
di attesa dei metadati passa da 600 pause di 100 ms a 50 pause. Dopo 5 secondi
senza risposta viene quindi usato il percorso di ripiego già presente nel gioco.
La stessa copia aggiornata ha mostrato il menu offline entro 22 secondi dal
comando di avvio e ha caricato il garage con i progressi esistenti. Una prova
sulle istruzioni ARM verifica anche che risposta ed errore terminino subito
l'attesa sia prima sia dopo la modifica.

Per chi gestisce una propria copia dell'APK 1.8.7 ARMv7, il sorgente facoltativo
è in [scripts/go-offline](https://github.com/MerryDoThings/nostalgia-game-hub/blob/main/scripts/go-offline/patch_metadata_wait.py):

```powershell
py scripts/go-offline/patch_metadata_wait.py copia.apk copia-attesa-breve-unsigned.apk
```

Lo script controlla l'impronta della funzione prima di cambiare una sola
istruzione. Conserva gli altri file, le altre istruzioni e le eventuali
correzioni già presenti al caricamento dei salvataggi. Produce un APK **non
firmato** e un rapporto locale; non installa né avvia il gioco.

Per aggiornare una copia installata occorre firmare l'APK con un certificato
compatibile con quello già usato. Conserva i salvataggi e il pacchetto precedente
prima di una prova. Non disinstallare per superare un conflitto di firma: perderesti
i dati dell'app. Un APK fornito allo script può contenere dati personali nei suoi
asset; conserva l'output localmente.

La prova riguarda menu e garage con contenuti già presenti. I nuovi contenuti
da scaricare richiedono la connessione. Il launcher e lo strumento di copia dei
salvataggi non applicano automaticamente questa modifica.

La prova è stata ripetuta dopo una chiusura completa di BlueStacks, verificando
un nuovo salvataggio integro con gli stessi progressi. È stata caricata anche
una pista di Seedway offline. Per riprodurre la prova puoi usare la
[modalità aereo di BlueStacks](https://support.bluestacks.com/hc/it/articles/11988217610381-Come-abilitare-o-disabilitare-la-modalit%C3%A0-aereo-su-BlueStacks-5).

Durante il controllo è stato osservato anche un arresto grafico del Game Center
di BlueStacks. Se l'emulatore rimane sulla Home, apri Go dalla sua icona: nella
prova il gioco ha mostrato il menu e ha conservato i progressi.
