using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Handles setting up the text + required items for the flashcards 

public class SetupCurrTaskOverview : MonoBehaviour
{
    public ManageStepFlashcardMulti currFlashcard;
    public ManageStepFlashcardSolo prevFlashcard;
    public ManageStepFlashcardSolo nextFlashcard;
    public void SetupCurrTask(Step currStep, Center_of_Objs centerScript = null)
    {
        currFlashcard.InitializeFlashcad(currStep);
        List<string> reqList;
        if (currStep.SubSteps.Count > 0)
        {
            reqList = currStep.SubSteps[currStep.CurrSubStepIndex].RequiredItems;
        } else
        {
            reqList = currStep.RequiredItems;
        }
        if (centerScript != null)
        {
            centerScript.ClearObjs();
            foreach (string str in reqList)
            {
                centerScript.AddObj(str, GameObject.Find(str));
            }
        }
    }

    public void SetupPrevTask(Step prevStep)
    {
        prevFlashcard.InitializeFlashcard(prevStep);
    }
    public void SetupNextTask(Step nextStep)
    {
        nextFlashcard.InitializeFlashcard(nextStep);
    }
}
