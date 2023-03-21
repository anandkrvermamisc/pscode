
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using PluralsightBot.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PluralsightBot.Models.FacebookModels;

namespace PluralsightBot.Dialogs
{
	public class BugTypeDialog : ComponentDialog
	{
		#region Variables
		private readonly BotServices _botServices;
		#endregion

		public BugTypeDialog(string dialogId, BotServices botServices) : base(dialogId)
		{
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
			AddDialog(new WaterfallDialog($"{nameof(BugTypeDialog)}.mainFlow", waterfallSteps));

			// Set the starting Dialog
			InitialDialogId = $"{nameof(BugTypeDialog)}.mainFlow";
		}

		private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			var result = await _botServices.Dispatch.RecognizeAsync<LuisModel>(stepContext.Context, cancellationToken);
			var value = string.Empty;
			var bugOuter = result.Entities.BugTypes_List?.FirstOrDefault();
			if (bugOuter != null)
				value = bugOuter?.FirstOrDefault() != null ? bugOuter?.FirstOrDefault() : value;

			if (Common.BugTypes.Any(s => s.Equals(value, StringComparison.OrdinalIgnoreCase)))
			{
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Yes! {value} is a Bug Type!"), cancellationToken);
				// Create Facebook Response
				stepContext.Context.Activity.Text = "";
				var replyMessage = stepContext.Context.Activity;

				var facebookMessage = new FacebookSendMessage
				{
					notification_type = "REGULAR",
					attachment = new FacebookAttachment()
				};
				facebookMessage.attachment.Type = FacebookAttachmentTypes.template;
				facebookMessage.attachment.Payload = new FacebookPayload
				{
					TemplateType = FacebookTemplateTypes.generic
				};
				var bugType = new FacebookElement
				{
					Title = value
				};
				if (value != null)
					switch (value.ToLower())
					{
						case "security":
							bugType.ImageUrl = "https://c1.staticflickr.com/9/8604/16042227002_1d00e0771d_b.jpg";
							bugType.Subtitle = "This is a description of the security bug type";
							break;
						case "crash":
							bugType.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/5/50/Windows_7_BSOD.png";
							bugType.Subtitle = "This is a description of the crash bug type";
							break;
						case "power":
							bugType.ImageUrl = "https://www.publicdomainpictures.net/en/view-image.php?image=1828&picture=power-button";
							bugType.Subtitle = "This is a description of the power bug type";
							break;
						case "performance":
							bugType.ImageUrl = "https://commons.wikimedia.org/wiki/File:High_Performance_Computing_Center_Stuttgart_HLRS_2015_07_Cray_XC40_Hazel_Hen_IO.jpg";
							bugType.Subtitle = "This is a description of the performance bug type";
							break;
						case "usability":
							bugType.ImageUrl = "https://commons.wikimedia.org/wiki/File:03-Pau-DevCamp-usability-testing.jpg";
							bugType.Subtitle = "This is a description of the usability bug type";
							break;
						case "seriousbug":
							bugType.ImageUrl = "https://commons.wikimedia.org/wiki/File:Computer_bug.svg";
							bugType.Subtitle = "This is a description of the serious bug type";
							break;
						case "other":
							bugType.ImageUrl = "https://commons.wikimedia.org/wiki/File:Symbol_Resin_Code_7_OTHER.svg";
							bugType.Subtitle = "This is a description of the other bug type";
							break;
						default:
							break;
					}

				facebookMessage.attachment.Payload.Elements = new FacebookElement[] { bugType };
				replyMessage.ChannelData = facebookMessage;
				await stepContext.Context.SendActivityAsync(replyMessage, cancellationToken);
			}
			else
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"No that is not a bug type"), cancellationToken);

			return await stepContext.NextAsync(null, cancellationToken);
		}

		private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			return await stepContext.EndDialogAsync(null, cancellationToken);
		}
	}
}


