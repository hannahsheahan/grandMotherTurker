using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;


public class ExperimentConfig
{
    /// <summary>
    /// This script contains all the experiment configuration details
    /// e.g. experiment type, trial numbers, ordering and randomisation, trial start and end locations. 
    /// This is a simplified, general purpose version for creating different behavioural experiments in Unity.
    /// Notes:  - Variables should eventually be turned private. Some currently public for ease of communication with DataController.
    ///         - Note that eventually the menu scenes could be assembled into a single scene, which would require small alterations to this script. Unimportant development not on immediate horizon.
    /// Author: Hannah Sheahan, sheahan.hannah@gmail.com
    /// Date: 08/11/2018
    /// </summary>


    // Scenes/mazes
    private const int setupAndCloseTrials = 7;     // Note: there must be 7 extra trials in trial list to account for Persistent, InformationScreen, BeforeStartingScreen, ConsentScreen, StartScreen, Instructions and Exit 'trials'.
    private const int restbreakOffset = 1;         // Note: makes specifying restbreaks more intuitive
    private const int getReadyTrial = 1;           // Note: this is the get ready screen after the practice
    private const int setupTrials = setupAndCloseTrials - 1;
    private int totalTrials;
    private int practiceTrials;
    private int restFrequency;
    private int nbreaks;
    private string[] trialMazes;
    private string[] possibleMazes;               
    private int sceneCount;
    private int blockLength;

    // Timer variables (currently public since few things go wrong if these are changed externally, since these are tracked in the data, but please don't change these externally...)
    public float maxResponseTime;
    public float preDisplayCueTime;
    public float finalGoalHitPauseTime;
    public float displayCueTime;
    public float goCueDelay;
    public float displayMessageTime;
    public float errorDwellTime;
    public float restbreakDuration;
    public float getReadyDuration;
    public float pausePriorFeedbackTime;
    public float feedbackFlashDuration;
    private float dataRecordFrequency;       // NOTE: this frequency is referred to in TrackingScript.cs for player data and here for state data

    // Randomisation of trial sequence
    public System.Random rand = new System.Random();

    // Question and answer data
    public QuestionData[] trialQuestionData;                              // final order of questions we WILL include
    public List<QuestionData> allQuestions = new List<QuestionData>();    // all possible questions that we could include
    public List<QuestionData> practiceQuestions = new List<QuestionData>();    // all possible questions that we could include

    // Preset experiments
    public string experimentVersion;
    private int nExecutedTrials; 
               
    // ********************************************************************** //
    // Use a constructor to set this up
    public ExperimentConfig()
    {
        experimentVersion = "mturk_pilot";
        //experimentVersion = "micro_debug";
        //experimentVersion = "singleblock_labpilot";


        // Set these variables to define your experiment:
        switch (experimentVersion)
        {
            case "mturk_pilot":       // ----Full 4 block learning experiment-----
                practiceTrials = 2 + getReadyTrial;
                nExecutedTrials = 30;
                totalTrials = nExecutedTrials + setupAndCloseTrials + practiceTrials;        // accounts for the Persistent, StartScreen and Exit 'trials'
                restFrequency = 31 + restbreakOffset;                               // Take a rest after this many normal trials
                restbreakDuration = 30.0f;                                          // how long are the imposed rest breaks?
                blockLength = 30;
                break;

            case "singleblock_labpilot":   // ----Mini 1 block test experiment-----
                practiceTrials = 1 + getReadyTrial;
                nExecutedTrials = 16;
                totalTrials = nExecutedTrials + setupAndCloseTrials + practiceTrials;        // accounts for the Persistent, StartScreen and Exit 'trials'
                restFrequency = 20 + restbreakOffset;                          // Take a rest after this many normal trials
                restbreakDuration = 5.0f;                                        // how long are the imposed rest breaks?
                blockLength = 16;
                break;

            case "micro_debug":            // ----Mini debugging test experiment-----
                practiceTrials = 2 + getReadyTrial;
                nExecutedTrials = 3;                                         // note that this is only used for the micro_debug version
                totalTrials = nExecutedTrials + setupAndCloseTrials + practiceTrials;        // accounts for the Persistent, StartScreen and Exit 'trials'
                restFrequency = 5 + restbreakOffset;                            // Take a rest after this many normal trials
                restbreakDuration = 5.0f;                                       // how long are the imposed rest breaks?
                blockLength = nExecutedTrials;
                break;

            default:
                Debug.Log("Warning: defining an untested trial sequence");
                break;
        }

        if (restFrequency - restbreakOffset < blockLength)
        {
            Debug.Log("Warning: rest breaks not allocated properly in trial sequence");
        }

        // Figure out how many rest breaks we will have and add them to the trial list
        nbreaks = Math.Max((int)((totalTrials - setupAndCloseTrials - practiceTrials) / restFrequency), 0);  // round down to whole integer
        totalTrials = totalTrials + nbreaks;

        // Timer variables (measured in seconds) - these can later be changed to be different per trial for jitter etc
        dataRecordFrequency = 0.06f;
        getReadyDuration = 5.0f;                      // how long we have to 'get ready' after the practice, before main experiment begins

        // Note that when used, jitters ADD to these values - hence they are minimums. See GameController for the usage/meaning of these variables.
        maxResponseTime   = 90.0f;                    // 90f
        preDisplayCueTime = 1.0f;               
        displayCueTime    = 0.0f;
        goCueDelay        = 0.1f;                     // they have to spend at least this much time reading before they can respond                 
        finalGoalHitPauseTime  = 0.2f;           
        displayMessageTime     = 1.5f;
        errorDwellTime         = 1.5f;                // Note: should be at least as long as displayMessageTime
        pausePriorFeedbackTime = 0.0f;
        feedbackFlashDuration  = 0.2f;                // duration that colour button feedback is shown for

        // Allocate space for the ordered questions, answers and associated stimuli
        trialMazes = new string[totalTrials];
        trialQuestionData = new QuestionData[totalTrials];
        for (int i=0; i < totalTrials; i++) 
        {
            trialQuestionData[i] = new QuestionData(0);
        }

        // Generate a list of all the possible (player or star) spawn locations
        GeneratePossibleSettings();

        // Define the start up menu and exit trials.   Note:  the other variables take their default values on these trials
        trialMazes[0] = "Persistent";
        trialMazes[1] = "InformationScreen";
        trialMazes[2] = "BeforeStartingScreen";
        trialMazes[3] = "ConsentScreen";
        trialMazes[4] = "StartScreen";
        trialMazes[5] = "InstructionsScreen";
        trialMazes[setupTrials + practiceTrials - 1] = "GetReady";
        trialMazes[totalTrials - 1] = "Exit";

        // Add in the practice trials
        AddPracticeTrials();

        // Generate the trial randomisation/list that we want.   Note: Ensure this is aligned with the total number of trials
        int nextTrial = System.Array.IndexOf(trialMazes, null);

        // Define the full trial sequence
        switch (experimentVersion)
        {
            case "mturk_pilot":       // ----Pilot 2 block question-answering experiment-----

                //----  block 1 (I think trials repeat across blocks so we have to do it this way without rest breaks)
                nextTrial = AddTrainingBlock(nextTrial, blockLength);

                break;

            case "singleblock_labpilot":   // ----Mini 1 block test experiment-----
                //---- training block 1
                nextTrial = AddTrainingBlock(nextTrial, blockLength);
                break;

            case "micro_debug":            // ----Mini debugging test experiment-----
                nextTrial = AddTrainingBlock(nextTrial, blockLength);
                break;

            default:
                Debug.Log("Warning: defining an untested trial sequence");
                break;
        }

        // For debugging: print out the final trial sequence in readable text to check it looks ok
        //PrintTrialSequence();
    }

    // ********************************************************************** //

    private void PrintTrialSequence()
    {
        // This function is for debugging/checking the final trial sequence by printing to console
        for (int trial = 0; trial < totalTrials; trial++)
        {
            Debug.Log("Trial " + trial + ", Maze: " + trialMazes[trial] + ", Stimulus: " + trialQuestionData[trial].stimulus);
            Debug.Log("--------");
        }
    }

    // ********************************************************************** //

    private void AddPracticeTrials()
    {
        // Add in the practice/familiarisation trials
        /*
        for (int trial = setupTrials; trial < setupTrials + practiceTrials - 1; trial++)
        {
            SetTrial(trial, practiceQuestions[rand.Next(practiceQuestions.Count)]);      // for now just give a random trial for practice
            trialMazes[trial] = "Practice";                                    // reset the maze for a practice trial
        }
        */

        // this is a hack for the away day in which the ordering of practice trials can be fixed ***HRS (15/03/2019)
        for (int trial = setupTrials; trial < setupTrials + practiceTrials - 1; trial++)
        {
            SetTrial(trial, practiceQuestions[trial - setupTrials]);      // for now just give a random trial for practice
            trialMazes[trial] = "Practice";                                    // reset the maze for a practice trial
        }

    }

    // ********************************************************************** //

    private void GeneratePossibleSettings()
    {
        SetPossibleQuestions();

        // Get all the possible mazes/scenes in the build that we can work with
        sceneCount = SceneManager.sceneCountInBuildSettings;
        possibleMazes = new string[sceneCount];
        for (int i = 0; i < sceneCount; i++)
        {
            possibleMazes[i] = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
        }
    }

    // ********************************************************************** //

    private int RestBreakHere(int firstTrial)
    {
        // Insert a rest break here and move to the next trial in the sequence
        trialMazes[firstTrial] = "RestBreak";
        return firstTrial + 1;
    }

    // ********************************************************************** //

    private int AddTrainingBlock(int nextTrial, int numberOfTrials)
    {
        // Note that we can use the below function (which shuffles over all bracketed trials), to shuffle subsets of trials, or shuffle within a context etc
        nextTrial = ShuffleTrialOrderAndStoreBlock(nextTrial, numberOfTrials);
        return nextTrial;
    }

    // ********************************************************************** //

    private void SetTrial(int trial, QuestionData questionData)
    {
        // This function writes the trial number indicated by the input variable 'trial'.

        // Check that we've inputted a valid trial number
        if ((trial < setupTrials - 1) || (trial == setupTrials - 1))
        {
            Debug.Log("Trial randomisation failed: cannot write to invalid trial number.");
        }
        else
        {
            // Write the question, answers, stimulus and scene
            trialQuestionData[trial] = questionData; 
            trialMazes[trial] = "MainTrial";
        }
    }

    // ********************************************************************** //

    private void SetPossibleQuestions()
    {
        // This function creates a list of possible questions (and answers) from which to generate trials.
        // Each possible question comes with several options for selectable answers, as well as a correct answer. 
        // This creates a list of (Q,PA,A) for all questions. This list is later shuffled appropriately to allocate trials to a sequence.

        // Notes: - add as many questions here as you like. 
        //        - Randomisation will reorder these and offer repeats if you haven't specified enough unique trials.
        //        - answerOrder will randomise which of the two answers appears on the left vs right (but only works for two answers)

        int answerOrder;
        int nPossibleAnswers;

        // Set up two practice questions

        // ---- Practice Question ---
        nPossibleAnswers = 2;
        QuestionData questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "This is a practice question. Do you understand how to select and submit your answer to each question?";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "yes";  //H1
        questiondata.answers[1 - answerOrder].answerText = "no";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        practiceQuestions.Add(questiondata);


        // ---- Practice Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "This is a practice question. Do you understand how to adjust your confidence level in your response?";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "yes";  //H1
        questiondata.answers[1 - answerOrder].answerText = "no";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        practiceQuestions.Add(questiondata);

        //==================================================
        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants are told a price for a product with a given feature (eg next day delivery), and then asked how much they would pay for the product with a different feature (eg 1 week delivery). They are  allowed to adjust the initial price by a fixed amount (the \"maximum allowable\") that varies between participants. \n The participants' decisions about how much to pay will...";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "depend on the maximum allowable adjustment";  //H1
        questiondata.answers[1 - answerOrder].answerText = "be independent of the maximum allowable adjustment";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);

        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants' attitudes to race, sexual orientation and body weight are measured from an online test of implicit attitudes. Data were aggregated over the past 13 years. Attitudes are classified as being close to neutral (e.g. indifference to an individual's sexual orientation) or far from neutral (e.g. displaying a negative attitude towards groups with a particular sexual orientation). \nOver the past 13 years, participants' attitudes about race have become...";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "closer to neutral, but those about body weight have changed to be further from neutral"; //H1
        questiondata.answers[1 - answerOrder].answerText = "closer to neutral, but those about race have changed to be further from neutral";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);

        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants are shown a container in the shape of a cup which is wider at the base than at the opening. Participants perceive the container as being more volumnious (i.e. able to hold more liquid) when it is...";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "the right way up"; //H1
        questiondata.answers[1 - answerOrder].answerText = "upside down";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);

        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants are given $5 each day for five days and need to spend this windfall on the same thing every day at roughly the same time.One group of participants were told to buy something for themselves, while another group was told to spend the money in order to do something good for another person (e.g.donating to the same charity each day), the \"giving\" condition. Participants were asked how happy they felt overall at the end of each day...";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "participants in the spending condition were less happy at day 5 than in the giving condition"; //H1
        questiondata.answers[1 - answerOrder].answerText = "participants in the giving condition were less happy at day 5 than in the spending condition";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);

        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants were shown sets of 3 objects in random colours (e.g. an orange pot).  After a delay, they were asked to turn a \"colour wheel\" to reproduce the exact colour of the object. Between the initial object presentation and the colour wheel task there was either short delay (short-term memory) or a long delay (long-term memory). An analysis technique was used to remove trials where participants guessed randomly. Participants will remember the colour better after...";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "a short delay than a long delay"; //H1
        questiondata.answers[1 - answerOrder].answerText = "a long delay than a short delay.";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);

        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants read short scenarios that described medical or legal situations. Each situation described the probability that evidence (e.g. results of a blood test) favoured a consequential verdict or diagnosis (e.g. athlete has been doping). However, they were also told that the evidence could arise for another reason with the same probability (e.g.ingesting a health food product). Participants were asked to report the likelihood of the consequential verdict or diagnosis given a positive or negative test result (e.g. positive or negative blood test). When told the test is positive, participants will believe that...";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "the evidence is irrelevant to the decision, and so the verdict or diagnosis cannot be trusted"; //H1
        questiondata.answers[1 - answerOrder].answerText = "the consequential explanation is more likely than not, even though there is another possible explanation";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants were shown two pairs of food items, one on the left and one on the right of the screen. Each pair always consisted of a top item and a bottom item. The top item was either a healthy but less tasty snack (e.g. a carrot) or a tasty but less healthy snack (e.g. a chocolate bar).  The bottom item was the same between both pairs, and was either (in the 'common disciplined' condition) a healthy item (e.g. an apple) or (in the 'common indulgent' condition) a tasty item (e.g. a bag of crisps). Participants chose which pair they preferred to eat. ";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants were more likely to choose the pair with the healthy/less tasty choice (e.g. the carrot) over the tasty/less healthy choice (e.g. the chocolate bar) in the \"common-disciplined\" condition than the \"common indulgent\" condition."; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants were less likely to choose the pair with the healthy/less tasty choice (e.g. the carrot) over the tasty/less healthy choice (e.g. the chocolate bar) in the \"common-disciplined\" condition than the \"common indulgent\" condition";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "In an initial familiarisation phase, 12 month old Infants were shown successive examples of previously unfamiliar objects (coffee makers and staplers). They were then told a fictitous name for one of the object categories (e.g. shown a stapler, and told \"this is a wug\"). Subsequently, they were shown a coffee maker and a stapler side by side, and asked which is the \"wug\". Their responses were measured by tracking whether they looked at the correct item. Infants learned better when ...";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "the initial familiarisation phase involved only a single object (e.g. all items were staplers)"; //H1
        questiondata.answers[1 - answerOrder].answerText = "the initial familiarisation phase involved a mixture of objects (e.g. both staplers and coffee makers)";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants who are good at recognising faces (\"super-recognisers\") and those who are bad at recognising faces (\"developmental prosopagnosics\") were asked to identify celebrity faces.  To make the task hard, only a part of the image (e.g. eyes or mouth) was shown through a small window.  The data were used to establish whether super-recognisers and prosopagnosics use the same information to recognise faces (e.g. both rely on the eyes, but super-recognisers are more sensitive) or different information (e.g. one group relies on the eyes and another on the mouth).";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Super-recognisers and prosopagnosics rely on the same information when judging faces"; //H1
        questiondata.answers[1 - answerOrder].answerText = "Super-recognisers and prosopagnosics rely on different information when judging faces";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants are told about Mr.X, who judges that a doctor of unknown gender is more likely to be a man than a woman.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants evaluate Mr. X negatively, because this seems unfair"; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants evaluate Mr. X positively, because this seems accurate";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants viewed a screen with a small cross in the middle. The outline of a square box appeared on the left or right of the screen, next to the cross.  Participants were asked to respond as quickly as possible to an asterisk that appeared either within the box or on the opposite side of the screen. The asterisk was sometimes preceded by a beeping sound.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants are faster to respond when the asterisk is inside the box when the beep occurs"; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants are slower to respond to the asterisk inside the box when the beep occurs";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants viewed pictures of faces that were manipulated by a computer to look more or less attractive and more or less competent.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Faces were manipulated to look more competent were perceived as more masculine"; //H1
        questiondata.answers[1 - answerOrder].answerText = "Faces that were manipulated to look competent were perceived as more feminine";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);

        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants are shown a container in the shape of a cup which is wider at the base than at the opening.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants perceive the container as being more volumnious (i.e. able to hold more liquid) when it is the right way up"; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants perceive the container as being more volumnious  (i.e. able to hold more liquid)  when it is upside down";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);

        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants were asked to recall headline news stories that occured during a 2-year period preceding the 2016 US Presidential Election.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "News events that occurred closer together in time were more likely to be recalled successively (one after another), even if they were not similar in content"; //H1
        questiondata.answers[1 - answerOrder].answerText = "News events that occurred closer together in time were not more likely to be recalled successively (one after another), beyond similarity in content";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Chimpanzees played a game in which they were handed a token that could provide food. They could either place the token in a box to receive a single banana slice (\"safe\" choice) or make a \"risky\" choice. In the  \"social risk\" condition, the risky choice was to pass the token to a chimpanzee partner in the neighbouring cage. If the partner placed the token in a \"prosocial box\", both chimpanzees received 1 banana slice. If she placed the token in a \"selfish box\", the partner received 2 banana slices and the other received nothing. In the \"nonsocial risk\" condition, the first chimpanzee could place the token in an apparatus that randomly yielded 1 or 2 rewards.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "The chimpanzees prefer to make safe choices in the \"nonsocial risk\" condition"; //H1
        questiondata.answers[1 - answerOrder].answerText = "The chimpanzees prefer to make safe choices in the \"social risk\" condition";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants saw two images of food items, one on either side of the screen. They were asked to report which one they would prefer to eat. Their gaze position was monitored with an eye tracker.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants were more likely to choose an item that they looked at more."; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants were more likely to choose an item that they looked at less.";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);



        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "In a food processing factory, workers normally have a spray santizer bottle placed on their workstation. In one workroom in the factory (\"room 1\"), an additional less comfortable-to-use squeezable sanitizer bottle was placed on each workstation, along with the normal and more comfortable to use spray sanitizer bottle. In the other workroom in the factory (\"room 2\"), no changes were made and only the spray sanitizer bottle was present on workers’ workstations.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Workers in room 1 use the spray sanitizer bottle more compared with workers in room 2."; //H1
        questiondata.answers[1 - answerOrder].answerText = "Workers in room 2 use the spray sanitizer bottle more compared with workers in room 1.";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants viewed different colours and were asked to associate them with social labels (self or stranger). They then perfomed a memory task on the colours.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants were faster to respond to a colour that had been associated with themself, relative to colours that had been associated with other people (stranger)."; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants were faster to respond to a colour that had been associated with a stranger, relative to colours that had been associated with themself.";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Pre-school children were given descriptions of the scientific method, either using action terms, e.g. \"“Today we’re going to do science!\" (action group) or generic language, e.g. \"“Today we’re going to be scientists!\" (language group). Afterwards, both groups practiced the scientific method by smelling cups, guessing their contents and checking whether their guesses were correct. After introductory trials, the children were asked if they wanted to continue the practice or do something else.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Girls were more likely to continue the practice in the action group than the language group, but boys did not differ between groups."; //H1
        questiondata.answers[1 - answerOrder].answerText = "Girls were more likely to continue the practice in the language group than the action group, but boys did not differ between groups.";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);



        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Surveys were used to assess how readily cultural exchange students adapted to the culture of their host country. The researchers also measured the \"tightness\" of the culture in the home and host country, where \"tightness\" refers to the extent to which rigidly imposed social norms were adhered to.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "1) Students who visited countries with \"looser\" cultures adapted more readily. 2) Students coming from a \"tighter\" culture adapted more readily. ."; //H1
        questiondata.answers[1 - answerOrder].answerText = "1) Students who visited countries with \"tighter\" cultures adapted more readily. 2) Students coming from a \"looser\" country adapted more readily. ";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Writers and physicists were asked to keep a diary in which they noted down when they had good ideas, what they were doing at the time, and whether the idea constituted an \"ah-ha!\" moment.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Ideas that occurred when not thinking about the problem were more likely to be \"ah-ha!\" moments"; //H1
        questiondata.answers[1 - answerOrder].answerText = "Ideas that occurred when not thinking about the problem were less likely to be \"ah-ha!\" moments";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants wore an unobtrusive audio recording device as they went about their daily lives. Several times a day, they also completed questionnaires about their moment-by-moment personality. Independent raters measured how extroverted/ introverted and agreeable/ disagreeable they were, based on the audio recordings.  The ratings were used to ask how much insight participants had into their own personality at each moment - whether they knew “what they were like”.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants had good insight into their levels of extroversion/introversion, weaker insight into how agreeable/disagreeable they were"; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants had good insight into their levels of agreeableness/disagreeableness, but weaker insight into how introverted/extroverted they were";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);

        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Estimates of ability in STEM (science, technology, engineering and maths) subjects from standardised scores taken when participants were aged 13 were used to predict their later degree of “eminence” 35 years later. Eminence was defined as having reached the pinnacle of their field in academic, medicine, law, business or a related profession.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants with high scores in STEM subjects were found to be more eminent in STEM-related disciplines 35 years later."; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants with high scores in STEM subjects were not found to be more eminent in STEM-related disciplines 35 years later.";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "6-month old infants were shown two objects on the left and right of a platform - a ball and a doll’s head.  After becoming familiar with the objects, they were each placed behind a screen. One of the screens was then lifted to reveal either the object that had been placed behind it(possible event) or the other object (impossible event). Infants’ surprise at the possible and impossible events was assessed by measuring how long they looked at the object in the possible and impossible conditions.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Babies looked longer at the surprising event, but only when the head is the right way up."; //H1
        questiondata.answers[1 - answerOrder].answerText = "Babies looked longer at the surprising event, but only when the head is upside down.";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants viewed coloured bars on a computer screen. The bars had different tilt (angle of orientation) and colour. They were asked (“probed”) to report the colour and orientation of one of the bars by turning a wheel. This allowed the researchers to assess the errors participants made in colour judgment and orientation judgment, and test whether they were correlated, a measure called “binding”. The bars were viewed in two conditions in which attention was manipulated in different ways. In the “split” condition, attention was split across two different bars, and participants did not know which would be probed. In the “shift” condition, attention was moved from one bar to the other just before they were presented.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Estimates of binding were lower in the split than the shift condition."; //H1
        questiondata.answers[1 - answerOrder].answerText = "Estimates of binding were lower in the shift than the split condition.";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);



        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants viewed two successive screens, each with lots of small items (dots, lines or shapes) on it.  They were asked to judge which screen contained the greatest number of items.On one of the two screens, spatially adjacent items were grouped together.In the similarity condition, this was achieved by making them the same colour (e.g. a patch of red items and a patch of blue items), whereas in the connectedness condition, this was achieved by connecting items into small groups with a line (like in “join the dots”).";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants underestimated the number of dots on the screen in the connectedness condition, but not in the similarity condition."; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants underestimated the number of dots on the screen in the similarity condition, but not in the connectedness condition.";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);



        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants were asked to predict the outcome of several recently published psychology experiments.They selected which of the two tested hypotheses they believed was more likely, and did not observe the scientific data.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants were able to correctly predict the scientific outcome of the experiment"; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants were at chance (50/50) at correctly predicting the scientific outcome of the experiment";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);


        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants in this study were high - school leavers who either went on to further schooling (group 1), or transitioned straight to work (group 2). 6 years after graduatioon, the researchers measured a personality trait known as conscientiousness in both groups.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants who joined the workforce immediately after high school displayed increased conscientiousness relative to those pursuing further education. "; //H1
        questiondata.answers[1 - answerOrder].answerText = "Participants who joined the workforce immediately after high school displayed decreased conscientiousness relative to those pursuing further education.";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);

        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Participants' brains size and IQ was measured.  The correlation between brain size and IQ was tested.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Participants with larger brains have higher IQ"; //H1
        questiondata.answers[1 - answerOrder].answerText = "There is no association between brain size and IQ";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);

        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "Pupillary contagion describes a phenomenon whereby one person's pupil (the black part of their eye) grows larger (dilates) when another person's pupil also dilates.  This study investigated pupillary contagion and gaze direction whilst individuals with autistic spectrum disorder (ASD) viewed faces.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Individuals with ASD show normal pupillary contagion, but abnormal gaze towards the eyes of the face in the picture"; //H1
        questiondata.answers[1 - answerOrder].answerText = "Individuals with autism show abnormal pupillary contagion, but normal gaze to the eyes of the individual shown in the picture";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);




        /*
        // ---- Question ---
        questiondata = new QuestionData(nPossibleAnswers);

        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "A study tracks changes in personality after students leave high school. The study investigates whether their choice to pursue further schooling or to transition to work immediately after graduation affects measures of conscientiousness 6 years post-graduation.";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = "Joining the workforce immediately after high school increases conscientiousness compared to pursuing further education. "; //H1
        questiondata.answers[1 - answerOrder].answerText = "";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);
        */


        /*
        answerOrder = rand.Next(nPossibleAnswers);
        questiondata.questionText = "...";
        questiondata.stimulus = "";
        questiondata.answers[answerOrder].answerText = " "; //H1
        questiondata.answers[1 - answerOrder].answerText = " ";  //H0
        questiondata.answers[answerOrder].isCorrect = true;
        allQuestions.Add(questiondata);
        */




    }

    // ********************************************************************** //

    public int ShuffleTrialOrderAndStoreBlock(int firstTrial, int blockLength)
    {
        // This function shuffles the prospective trials from firstTrial to firstTrial+blockLength and stores them.

        bool randomiseOrder = true;
        int n = allQuestions.Count;
        QuestionData questionData;

        // first check that we have specified enough possible questions for generating all-unique trials in this block 
        if (allQuestions.Count < blockLength) 
        {
            Debug.Log("Warning: we have not specified enough unique trials to fill this block. Trials will repeat.");
        }

        if (randomiseOrder)
        {
            // Perform the Fisher-Yates algorithm for shuffling array elements in place 
            for (int i = 0; i < n; i++)
            {
                int k = i + rand.Next(n - i); // select random index in array, less than n-i

                // shuffle questions to ask, keeping their associated data together
                QuestionData oneQuestion = allQuestions[k];
                allQuestions[k] = allQuestions[i];
                allQuestions[i] = oneQuestion;
            }
        }

        // Store the randomised trial order (reuse random trials if we haven't specified enough unique ones)
        for (int i = 0; i < blockLength; i++)
        {
            if (i < n) 
            {
                questionData = allQuestions[i];
            }
            else     // randomly select a trial to be repeated in the trial sequence
            {
                questionData = allQuestions[rand.Next(allQuestions.Count)];
            }
            SetTrial(i + firstTrial, questionData);
        }

        return firstTrial + blockLength;
    }

    // ********************************************************************** //

    public float JitterTime(float time)
    {
        // Note: currently unused and untested
        // jitter uniform-randomly from the min value, to 50% higher than the min value
        return time + (0.5f*time)* (float)rand.NextDouble();
    }

    // ********************************************************************** //
    // Get() and Set() Methods
    // ********************************************************************** //

    public int GetTotalTrials()
    {
        return totalTrials;
    }

    // ********************************************************************** //

    public float GetDataFrequency()
    {
        return dataRecordFrequency;
    }

    // ********************************************************************** //

    public string GetTrialMaze(int trial)
    {
        return trialMazes[trial];
    }

    // ********************************************************************** //

    public string GetStimulus(int trial)
    {
        return trialQuestionData[trial].stimulus;
    }

    // ********************************************************************** //

    public string[] GetPossibleAnswers(int trial) 
    {
        string[] answerTexts = new string[trialQuestionData[trial].answers.Length];

        for (int i = 0; i < answerTexts.Length; i++) 
        {
            answerTexts[i] = trialQuestionData[trial].answers[i].answerText;
        }

        return answerTexts;
    }

    // ********************************************************************** //

    public string GetAnswer(int trial)
    {
        int nPossibleAnswers;
        string answer = "";
        nPossibleAnswers = trialQuestionData[trial].answers.Length;

        for (int i = 0; i < nPossibleAnswers; i++) 
        { 
            if (trialQuestionData[trial].answers[i].isCorrect) 
            {
                answer = trialQuestionData[trial].answers[i].answerText;
            }
        }
        return answer;
    }

    // ********************************************************************** //

    public string GetQuestion(int trial)
    {
        return trialQuestionData[trial].questionText;
    }

    // ********************************************************************** //

}