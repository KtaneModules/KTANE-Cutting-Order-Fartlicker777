using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class CuttingOrder : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public VoltageMeterReader VMR;
   public KMColorblindMode ColorblindMode;
   private bool cbMode = false;


   public TextMesh[] CBText;

   public KMSelectable[] Wires;
   public GameObject[] TopCuts;
   public GameObject[] MidCuts;
   public GameObject[] BotCuts;
   public Material[] Colors;

   public Material[] RandoColors;

   public GameObject[] LEDs;
   public Material[] LEDColors;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;

   bool[] IsCut = new bool[8];
   bool[] Struck = new bool[8];

   string[] Rows = {
      "R1O2Y3G4B5V678",
      "6KBYWVG4325O87",
      "VK863BOW54Y2R1",
      "48576BK1RV3WGY",
      "6BYK81ORG3725V",
      "G23Y46KB7OV85R",
      "7G3O45Y1B2KV6W",
      "6785W3KOYVR1B2",
      "125YORW87V6K4B",
      "GKB8W4172RY63O" };

   string ColorOrder = "ROYGBVWK";

   int CurrentWireToCut;
   int StepInSequence;
   int RuleNumber;

   int[] ColorsNumerically = { 0, 1, 2, 3, 4, 5, 6, 7};


   void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;

      foreach (KMSelectable Wire in Wires) {
         Wire.OnInteract += delegate () { WireCut(Wire); return false; };
      }

      cbMode = ColorblindMode.ColorblindModeActive;
      //button.OnInteract += delegate () { buttonPress(); return false; };

   }

   void WireCut (KMSelectable Wire) {
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, Wire.transform);
      IsCut[Array.IndexOf(Wires, Wire)] = true;
      if (Array.IndexOf(Wires, Wire) == CurrentWireToCut) {
         CalculateCurrentWireToCut();
         LEDs[Array.IndexOf(Wires, Wire)].GetComponent<MeshRenderer>().material = LEDColors[0];
      }
      else {
         Strike();
         Struck[Array.IndexOf(Wires, Wire)] = true;
         LEDs[Array.IndexOf(Wires, Wire)].GetComponent<MeshRenderer>().material = LEDColors[1];
      }
      Wire.gameObject.SetActive(false);
      switch (Rnd.Range(0, 3)) {
         case 0:
            TopCuts[Array.IndexOf(Wires, Wire)].SetActive(true);
            TopCuts[Array.IndexOf(Wires, Wire)].transform.GetChild(Rnd.Range(0, 2)).gameObject.SetActive(true);
            break;
         case 1:
            MidCuts[Array.IndexOf(Wires, Wire)].SetActive(true);
            MidCuts[Array.IndexOf(Wires, Wire)].transform.GetChild(Rnd.Range(0, 2)).gameObject.SetActive(true);
            break;
         case 2:
            BotCuts[Array.IndexOf(Wires, Wire)].SetActive(true);
            BotCuts[Array.IndexOf(Wires, Wire)].transform.GetChild(Rnd.Range(0, 2)).gameObject.SetActive(true);
            break;
         default:
            break;
      }
   }

   void OnDestroy () { //Shit you need to do when the bomb ends

   }

   void Activate () { //Shit that should happen when the bomb arrives (factory)/Lights turn on

   }

   void Start () { //Shit that you calculate, usually a majority if not all of the module
      ColorsNumerically = ColorsNumerically.Shuffle();
      for (int i = 0; i < 8; i++) {
         Wires[i].GetComponent<MeshRenderer>().material = Colors[ColorsNumerically[i]];
         TopCuts[i].GetComponent<MeshRenderer>().material = Colors[ColorsNumerically[i]];
         MidCuts[i].GetComponent<MeshRenderer>().material = Colors[ColorsNumerically[i]];
         BotCuts[i].GetComponent<MeshRenderer>().material = Colors[ColorsNumerically[i]];
         if (!cbMode) {
            CBText[i].gameObject.SetActive(false);
         }
         CBText[i].text = ColorOrder[ColorsNumerically[i]].ToString();
      }
      CalculateRuleNumber();

      Debug.LogFormat("[Cutting Order #{0}] Version 1.0.0.", ModuleId);

      Debug.LogFormat("[Cutting Order #{0}] Using row {1}.", ModuleId, RuleNumber + 1);
      CalculateCurrentWireToCut();

      //StartCoroutine(SolveAnim());
   }

   void CalculateRuleNumber () {
      if (VMR.GetVoltageMeterInt() != -1) {
         RuleNumber = VMR.GetVoltageMeterInt() - 1;
      }
      else if (Bomb.GetSerialNumberLetters().ToArray().Any(x => x == 'D' || x == 'E' || x == 'A' || x == 'F')) {
         RuleNumber = 0;
      }
      else if (ColorsNumerically[0] == 7) {
         RuleNumber = 1;
      }
      else if (Mathf.Abs(Array.IndexOf(ColorsNumerically, 0) - Array.IndexOf(ColorsNumerically, 3)) == 1) {
         RuleNumber = 2;
      }
      else if (Array.IndexOf(ColorsNumerically, 7) < Array.IndexOf(ColorsNumerically, 0)) {
         RuleNumber = 3;
      }
      else if (Array.IndexOf(ColorsNumerically, 6) < Array.IndexOf(ColorsNumerically, 5)) {
         RuleNumber = 4;
      }
      else if (Mathf.Abs(Array.IndexOf(ColorsNumerically, 1) - Array.IndexOf(ColorsNumerically, 2)) == 1) {
         RuleNumber = 5;
      }
      else if (Mathf.Abs(Array.IndexOf(ColorsNumerically, 1) - Array.IndexOf(ColorsNumerically, 4)) == 3) {
         RuleNumber = 6;
      }
      else if (Math.IsPrime(Array.IndexOf(ColorsNumerically, 2) + 1)) {
         RuleNumber = 7;
      }
      else if (Mathf.Abs(Array.IndexOf(ColorsNumerically, 6) - Array.IndexOf(ColorsNumerically, 7)) == 1) {
         RuleNumber = 8;
      }
      else {
         RuleNumber = 9;
      }
   }

   void CalculateCurrentWireToCut () {
      StartingPoint:
      bool WillSolve = true;
      for (int i = 0; i < 8; i++) {
         if (!IsCut[i]) {
            WillSolve = false;
         }
      }

      if ((WillSolve || StepInSequence > 13) && !ModuleSolved) {
         Solve();
         return;
      }


      if (ColorOrder.Contains(Rows[RuleNumber][StepInSequence].ToString())) {
         int TargetColor = ColorOrder.IndexOf(Rows[RuleNumber][StepInSequence].ToString());
         int TargetWire = Array.IndexOf(ColorsNumerically, TargetColor);
         if (IsCut[TargetWire]) {
            StepInSequence++;
            goto StartingPoint;
         }
         else {
            CurrentWireToCut = TargetWire;
         }
      }
      else {
         int TargetWire = int.Parse(Rows[RuleNumber][StepInSequence].ToString()) - 1;
         if (IsCut[TargetWire]) {
            StepInSequence++;
            goto StartingPoint;
         }
         else {
            CurrentWireToCut = TargetWire;
         }
      }
      Debug.LogFormat("[Cutting Order #{0}] Current wire is wire number {1}.", ModuleId, CurrentWireToCut + 1);
   }

   void Update () { //Shit that happens at any point after initialization

   }

   void Solve () {
      ModuleSolved = true;
      GetComponent<KMBombModule>().HandlePass();
      StartCoroutine(SolveAnim());
   }

   IEnumerator SolveAnim () {
      //yield return new WaitForSeconds(2f);
      Audio.PlaySoundAtTransform("HRS Solve", transform);
      float[] AnimTimes = { .3f, .1f, .1f, .1f, .3f, .3f, .3f, .3f};
      for (int i = 0; i < 8; i++) {
         LEDs[i].GetComponent<MeshRenderer>().material = RandoColors[Rnd.Range(0, 7)];
         yield return new WaitForSeconds(AnimTimes[i]);
      }
      yield return new WaitForSeconds(.2f);
      for (int i = 0; i < 8; i++) {
         if (Struck[i]) {
            LEDs[i].GetComponent<MeshRenderer>().material = LEDColors[1];
         }
         else if (IsCut[i]) {
            LEDs[i].GetComponent<MeshRenderer>().material = LEDColors[0];
         }
         else {
            LEDs[i].GetComponent<MeshRenderer>().material = LEDColors[2];
         }
         
         yield return new WaitForSeconds(AnimTimes[i]);
      }
   }

   void Strike () {
      GetComponent<KMBombModule>().HandleStrike();
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} # to cut that wire. Chain cuts by concatenating them. Use !{0} LUCKY if you're feeling lucky today, who knows what might happen.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
      if (Command == "LUCKY") {
         if (Rnd.Range(0, 10) == 0) {
            Solve();
         }
         else {
            int Rando = 0;
            do {
               Rando = Rnd.Range(0, 8);
            } while (IsCut[Rando]);
            Wires[Rando].OnInteract();
            yield break;
         }
         
      }
      if (Command.Any(x => !"12345678".Contains(x))) {
         yield return "sendtochaterror I don't understand!";
         yield break;
      }
      for (int i = 0; i < Command.Length; i++) {
         if (IsCut[int.Parse(Command[i].ToString())]) {
            continue;
         }
         Wires[int.Parse(Command[i].ToString()) - 1].OnInteract();
         yield return new WaitForSeconds(.1f);
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      while (!ModuleSolved) {
         Wires[CurrentWireToCut].OnInteract();
         yield return new WaitForSeconds(.1f);
      }
   }
}
