using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using PluralsightBot.Models;
using PluralsightBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PluralsightBot.Dialogs
{
    public class BugReportDialog : ComponentDialog
    {
        #region Variables
        private readonly StateService _stateService;
        #endregion  


        public BugReportDialog(string dialogId, StateService stateService) : base(dialogId)
        {
            _stateService = stateService ?? throw new System.ArgumentNullException(nameof(stateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                DescriptionStepAsync,
                CallbackTimeStepAsync,
                PhoneNumberStepAsync,
                BugStepAsync,
                SummaryStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(BugReportDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.description"));
            AddDialog(new DateTimePrompt($"{nameof(BugReportDialog)}.callbackTime", CallbackTimeValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.phoneNumber", PhoneNumberValidatorAsync));
            AddDialog(new ChoicePrompt($"{nameof(BugReportDialog)}.bug"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(BugReportDialog)}.mainFlow";
        }

        #region Waterfall Steps
        private async Task<DialogTurnResult> DescriptionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
	        var userProfile = (UserProfile)stepContext.Options;

	        if (string.IsNullOrEmpty(userProfile.Description))
	        {
		        return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.description",
			        new PromptOptions
			        {
				        Prompt = MessageFactory.Text("Enter a description for your report")
			        }, cancellationToken);
            }

	        return await stepContext.NextAsync(userProfile.Description, cancellationToken);
        }

        private async Task<DialogTurnResult> CallbackTimeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
	        var userProfile = (UserProfile)stepContext.Options;

            stepContext.Values["description"] = (string)stepContext.Result;

            if (userProfile.CallbackTime == null)
            {
	            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.callbackTime",
		            new PromptOptions
		            {
			            Prompt = MessageFactory.Text("Please enter in a callback time"),
			            RetryPrompt = MessageFactory.Text("The value entered must be between the hours of 9 am and 5 pm."),
		            }, cancellationToken);
            }

            return await stepContext.NextAsync(userProfile.CallbackTime, cancellationToken);
        }

        private async Task<DialogTurnResult> PhoneNumberStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
	        var userProfile = (UserProfile)stepContext.Options;
            if (stepContext.Result is DateTime time)
	            stepContext.Values["callbackTime"] = time;
            else
            {
	            var result = (List<DateTimeResolution>)stepContext.Result;
	            stepContext.Values["callbackTime"] = result?.FirstOrDefault() != null ? Convert.ToDateTime(result?.FirstOrDefault().Value) : null;
            }

            if (string.IsNullOrEmpty(userProfile.PhoneNumber))
	        {
		        return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.phoneNumber",
			        new PromptOptions
			        {
				        Prompt = MessageFactory.Text("Please enter in a phone number that we can call you back at"),
				        RetryPrompt = MessageFactory.Text("Please enter a valid phone number"),
			        }, cancellationToken);
            }

	        return await stepContext.NextAsync(userProfile.PhoneNumber, cancellationToken);
        }

        private async Task<DialogTurnResult> BugStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
	        var userProfile = (UserProfile)stepContext.Options;

            stepContext.Values["phoneNumber"] = (string)stepContext.Result;

            if (string.IsNullOrEmpty(userProfile.Bug))
            {
	            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.bug",
		            new PromptOptions
		            {
			            Prompt = MessageFactory.Text("Please enter the type of bug."),
			            Choices = ChoiceFactory.ToChoices(new List<string> { "Security", "Crash", "Power", "Performance", "Usability", "Serious Bug", "Other" }),
		            }, cancellationToken);
            }

            return await stepContext.NextAsync(userProfile.Bug, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
	        if (stepContext.Result is string bug)
		        stepContext.Values["bug"] = bug;
	        else
		        stepContext.Values["bug"] = ((FoundChoice)stepContext.Result).Value;

	        // Get the current profile object from user state.
            var userProfile = await _stateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Save all of the data inside the user profile
            userProfile.Description = (string)stepContext.Values["description"];
            userProfile.CallbackTime = (DateTime)stepContext.Values["callbackTime"];
            userProfile.PhoneNumber = (string)stepContext.Values["phoneNumber"];
            userProfile.Bug = (string)stepContext.Values["bug"];

            // Show the summary to the user
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Here is a summary of your bug report:"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Description: {userProfile.Description}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Callback Time: {userProfile.CallbackTime.ToString()}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Phone Number: {userProfile.PhoneNumber}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bug: {userProfile.Bug}"), cancellationToken);

            // Save data in userstate
            await _stateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
        #endregion

        #region Validators
        private Task<bool> CallbackTimeValidatorAsync(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var resolution = promptContext.Recognized.Value.First();
                DateTime selectedDate = Convert.ToDateTime(resolution.Value);
                TimeSpan start = new TimeSpan(9, 0, 0); //9 o'clock
                TimeSpan end = new TimeSpan(17, 0, 0); //5 o'clock
                if ((selectedDate.TimeOfDay >= start) && (selectedDate.TimeOfDay <= end))
                {
                    valid = true;
                }
            }
            return Task.FromResult(valid);
        }

        private Task<bool> PhoneNumberValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                valid = Regex.Match(promptContext.Recognized.Value, @"^(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$").Success;
            }
            return Task.FromResult(valid);
        }

        #endregion
    }
}
