import { Component, inject, input, ViewChild } from '@angular/core';
import { MessagesService } from '../../_services/messages.service';
import { TimeagoModule } from 'ngx-timeago';
import { FormsModule, NgForm } from '@angular/forms';

@Component({
  selector: 'app-member-messages',
  standalone: true,
  imports: [TimeagoModule, FormsModule],
  templateUrl: './member-messages.component.html',
  styleUrl: './member-messages.component.css'
})
export class MemberMessagesComponent {
  @ViewChild("messageForm") messageForm?: NgForm;
  messagesService = inject(MessagesService);
  username = input.required<string>();
  messageContent = "";

  sendMessage() {
    this.messagesService.sendMessageAsync(this.username(), this.messageContent).then(() => {
      this.messageForm?.reset();
    })
  }
}
