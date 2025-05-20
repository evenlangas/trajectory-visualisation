# Unity Trajectory Prediction Visualisering

## Oversikt

Dette prosjektet er laget for å visualisere menneskelige bevegelsesmønstre og prediksjoner i Unity. Det leser banedata fra JSON-filer og visualiserer:
1. De siste 30 posisjonene (3 sekunder tilbake i tid)
2. Nåværende posisjon
3. Predikerte fremtidige posisjoner

## Konfigurer scenen
   - Legg til et GameObject for å representere mennesket/agenten
   - Legg til `TrajectoryDataLoader.cs`-scriptet på dette objektet
   - Sett stien til JSON-filen i scriptets Inspector
   - Legg til `TrajectoryVisualizer.cs`-scriptet på samme eller et annet GameObject
   - Koble sammen referansene i inspektøren

## JSON Dataformat

JSON-filen følger denne strukturen:
```json
{
  "237": [
    {
      "t_id": 237,
      "t": 1734366772374948591,
      "x": -3.9366055159119253,
      "y": 11.672762905667721,
      "p_x": [-3.992249011993408, -4.006448268890381, ...],
      "p_y": [9.396206855773926, 9.470325469970703, ...]
    },
    ...
  ],
  "238": [
    ...
  ]
}
```

Hvor:
- Toppnivånøkler er bane-IDer
- Hver bane inneholder en liste med frames
- Hver frame har:
  - `t_id`: Bane-ID
  - `t`: Tidsstempel
  - `x`, `y`: Nåværende posisjon
  - `p_x`, `p_y`: Arrays med predikerte posisjoner

## Bruksanvisning

### Grunnleggende bruk
1. Velg GameObject med `TrajectoryDataLoader`-scriptet
2. Juster følgende innstillinger i inspektøren:
   - **JSON File Path**: Sti til JSON-datafilen
   - **Human Object**: Referanse til menneske/agent-objektet
   - **Playback Speed**: Avspillingshastighet
   - **Auto Play**: Start avspilling automatisk
   - **Loop Trajectories**: Gjenta alle baner kontinuerlig

### Visualiseringsinnstillinger
1. Velg GameObject med `TrajectoryVisualizer`-scriptet
2. Juster visualiseringsparametere:
   - **Max History Points**: Antall historiske punkter som vises
   - **History Line Color**: Farge på historikklinje
   - **Prediction Line Color**: Farge på prediksjonslinje
   - **Current Position Color**: Farge på nåværende posisjonsmarkør

### Under kjøring
- Banen spilles automatisk av hvis "Auto Play" er aktivert
- Hvis editor-modusen brukes, kan du bruke kontrollene for å:
   - Play/Pause avspilling
   - Justere avspillingshastighet
   - Laste inn andre JSON-filer

## Koordinatsystem

Merk at scriptene bruker følgende koordinatmapping:
- X i dataene tilsvarer X i Unity
- Y i dataene tilsvarer Z i Unity (siden Unity bruker Y som opp-akse)

For å endre denne mappingen, må du modifisere `UpdateHumanPosition`-metoden i `TrajectoryDataLoader.cs`-scriptet.

## Utvidelse av systemet

### Animering av figurer
For å animere en humanoidfigur:
1. Erstatt sylinderen med en animert karakter
2. Bruk posisjonsoppdateringer for å drive karakterens bevegelse
3. Legg til logikk for å bestemme gå/løpe-tilstander basert på hastighet

### Integrering i eksisterende prosjekter
1. Kopier scriptene `TrajectoryDataLoader.cs` og `TrajectoryVisualizer.cs` til ditt prosjekt
2. Følg oppsettsanvisningene ovenfor
3. Tilpass scriptene etter behov for ditt spesifikke prosjekt