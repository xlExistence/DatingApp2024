import { Component, inject } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-roles-modal',
  standalone: true,
  imports: [],
  templateUrl: './roles-modal.component.html',
  styleUrl: './roles-modal.component.css'
})
export class RolesModalComponent {
  bsModalRef = inject(BsModalRef);
  username = "";
  title = "";
  availableRoles: string[] = [];
  selectedRoles: string[] = [];
  rolesUpdated = false;

  updateChecked(checkedValue: string) {
    if (this.selectedRoles.includes(checkedValue)) {
      this.selectedRoles = this.selectedRoles.filter(r => r !== checkedValue);
    } else {
      this.selectedRoles.push(checkedValue);
    }
    this.rolesUpdated = true; // Esta línea hace que la llamada al API se ejectue cuando se modificaron los roles
  }

  onSelectRoles() {
    // this.rolesUpdated = true; // Esta línea hace que la llamada al API se ejectue siempre
    this.bsModalRef.hide();
  }
}
