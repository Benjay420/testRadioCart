using UnityEngine;
using TMPro; // Tilf�j denne linje hvis du vil bruge TextMeshPro til at vise omgangstallet

public class LapManager : MonoBehaviour
{
    [Header("Race Settings")]
    [Tooltip("Total number of laps required to finish the race.")]
    public int totalLaps = 3;

    [Tooltip("Total number of checkpoints the player must pass BEFORE crossing the finish line each lap.")]
    public int totalCheckpoints = 2; // Juster dette til antallet af checkpoints p� DIN bane

    [Header("UI Display (Optional)")]
    [Tooltip("Assign a TextMeshPro UI element here to display the current lap.")]
    public TextMeshProUGUI lapDisplay; // Tr�k dit UI Tekst-objekt hertil i Inspectoren

    // Private variabler til at holde styr p� spillerens status
    private int playerCurrentLap = 0;           // Hvilken omgang spilleren er p� (starter p� 1)
    private int playerLastCheckpointHit = 0;    // Nummeret p� det sidste checkpoint spilleren ramte p� denne omgang
    private bool raceFinished = false;          // Er l�bet f�rdigt?

    void Start()
    {
        // Initialiser v�rdier ved l�bets start
        playerCurrentLap = 1;
        playerLastCheckpointHit = 0;
        raceFinished = false;

        // Opdater UI'en f�rste gang
        UpdateLapUI();
        Debug.Log("Lap Manager Initialized. Race Started. Total Laps: " + totalLaps + ", Checkpoints per lap: " + totalCheckpoints);
    }

    // Denne funktion skal kaldes fra KartController.cs's OnTriggerEnter,
    // n�r karten rammer en trigger med et "CheckpointX" tag.
    public void KartHitCheckpoint(int checkpointNumber)
    {
        // Ignorer hvis l�bet er slut
        if (raceFinished) return;

        // Tjek om dette checkpoint er det *n�ste* i r�kken
        if (checkpointNumber == playerLastCheckpointHit + 1)
        {
            playerLastCheckpointHit = checkpointNumber;
            Debug.Log($"Player hit correct checkpoint: {checkpointNumber}/{totalCheckpoints}");

            // Valgfrit: Giv spilleren feedback (lyd, visuel effekt?)
        }
        else if (checkpointNumber > playerLastCheckpointHit + 1) {
             Debug.LogWarning($"Player hit checkpoint {checkpointNumber} out of order! Expected checkpoint {playerLastCheckpointHit + 1}.");
        }
        // Ignorer hvis de rammer et checkpoint, de allerede har passeret p� denne omgang (checkpointNumber <= playerLastCheckpointHit)
    }

    // Denne funktion skal kaldes fra KartController.cs's OnTriggerEnter,
    // n�r karten rammer en trigger med "FinishLine" tag'et.
    public void KartHitFinishLine()
    {
        // Ignorer hvis l�bet er slut
        if (raceFinished) return;

        // Tjek om alle checkpoints for DENNE omgang er blevet ramt
        if (playerLastCheckpointHit == totalCheckpoints)
        {
            // Alle checkpoints ramt - fuldf�r omgangen
            playerCurrentLap++;
            playerLastCheckpointHit = 0; // Nulstil checkpoint t�lleren til n�ste omgang

            Debug.Log($"Player completed Lap {playerCurrentLap - 1}! Now starting Lap {playerCurrentLap}.");

            // Tjek om l�bet er slut
            if (playerCurrentLap > totalLaps)
            {
                raceFinished = true;
                Debug.Log("PLAYER FINISHED THE RACE!");

                // ----- G�R NOGET N�R L�BET ER SLUT HER -----
                // F.eks.:
                // - Stop spillerens input?
                // - Vis en "Race Finished" sk�rm?
                // - Stop AI?
                // - Afspil en sejrs-lyd/animation?
                // ------------------------------------------

                // S�rg for at UI viser den sidste omgang korrekt (f.eks. 3/3 ikke 4/3)
                UpdateLapUI(true); // Send 'true' for at signalere race er slut
            }
            else
            {
                // L�bet er ikke slut, opdater bare UI til den nye omgang
                UpdateLapUI();
            }
        }
        else
        {
            // Spilleren krydsede m�lstregen, men har ikke ramt alle checkpoints endnu
             if (playerCurrentLap <= totalLaps) { // Undg� spam hvis l�bet er slut
                Debug.LogWarning($"Player crossed finish line but missed checkpoints! Last hit: {playerLastCheckpointHit}/{totalCheckpoints}");
             }
        }
    }

    // Opdaterer UI Teksten (hvis den er tildelt)
    void UpdateLapUI(bool raceJustFinished = false)
    {
        if (lapDisplay != null)
        {
            // Viser den aktuelle omgang, men aldrig mere end totalLaps
            int displayLap = raceJustFinished ? totalLaps : Mathf.Min(playerCurrentLap, totalLaps);
            lapDisplay.text = $"Lap: {displayLap} / {totalLaps}";

            // Tilf�j "Finished!" tekst hvis l�bet er slut
            if (raceFinished)
            {
                lapDisplay.text += "\nFinished!";
            }
        }
    }

    // Valgfri funktion til at hente den aktuelle omgang udefra
    public int GetCurrentLap()
    {
        return playerCurrentLap;
    }

     // Valgfri funktion til at tjekke om l�bet er f�rdigt udefra
    public bool IsRaceFinished() {
        return raceFinished;
    }
}