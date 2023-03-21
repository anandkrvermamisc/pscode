using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using PluralsightBot.Helpers;
using PluralsightBot.Models;
using PluralsightBot.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PluralsightBot.Dialogs
{
	public class MainDialog : ComponentDialog
    {
        #region Variables
        private readonly StateService _stateService;
        private readonly BotServices _botServices;
        #endregion  


        public MainDialog(StateService stateService, BotServices botServices) : base(nameof(MainDialog))
        {
            _stateService = stateService ?? throw new System.ArgumentNullException(nameof(stateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync
            };

            // Add Named Dialogs
            AddDialog(new GreetingDialog($"{nameof(MainDialog)}.greeting", _stateService));
            AddDialog(new BugReportDialog($"{nameof(MainDialog)}.bugReport", _stateService));
            AddDialog(new BugTypeDialog($"{nameof(MainDialog)}.bugType", _botServices));
            AddDialog(new WaterfallDialog($"{nameof(MainDialog)}.mainFlow", waterfallSteps));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(MainDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                // First, we use the dispatch model to determine which cognitive service (LUIS or QnA) to use.
                var recognizerResult = await _botServices.Dispatch.RecognizeAsync<LuisModel>(stepContext.Context, cancellationToken);

                // Top intent tell us which cognitive service to use.
                var topIntent = recognizerResult.TopIntent();

                switch (topIntent.intent)
                {
                    case LuisModel.Intent.GreetingIntent:
                        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
                    case LuisModel.Intent.NewBugReportIntent:
	                    var userProfile = new UserProfile();
	                    var bugReport = recognizerResult.Entities.BugReport_ML?.FirstOrDefault();
                        if (bugReport != null)
                        {
	                        var description = bugReport.Description?.FirstOrDefault();
	                        if (description != null)
	                        {
                                // Retrieve Description Text
                                userProfile.Description = bugReport._instance.Description?.FirstOrDefault() != null ? bugReport._instance.Description.FirstOrDefault().Text : userProfile.PhoneNumber;

                                // Retrieve Bug Text
                                var bugOuter = description.Bug?.FirstOrDefault();
		                        if (bugOuter != null)
			                        userProfile.Bug = bugOuter?.FirstOrDefault() != null ? bugOuter?.FirstOrDefault() : userProfile.Bug;
	                        }

                            // Retrieve Phone Number Text
	                        userProfile.PhoneNumber = bugReport.PhoneNumber?.FirstOrDefault() != null ? bugReport.PhoneNumber?.FirstOrDefault() : userProfile.PhoneNumber;

	                        // Retrieve Callback Time
                            userProfile.CallbackTime = bugReport.CallbackTime?.FirstOrDefault() != null ? AiRecognizer.RecognizeDateTime(bugReport.CallbackTime?.FirstOrDefault(), out string rawString) : userProfile.CallbackTime;
                        }

                        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.bugReport", userProfile, cancellationToken);
                    case LuisModel.Intent.QueryBugTypeIntent:
                        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.bugType", null, cancellationToken);
                    default:
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I'm sorry I don't know what you mean."), cancellationToken);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }









        



    }
}
