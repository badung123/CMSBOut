import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AgentService } from '../../core/services/agent.service';
import { Agent } from '../../core/models/api.models';
import { formatDateTime } from '../../shared/utils/text.utils';

@Component({
  selector: 'app-agents',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './agents.component.html',
  styleUrl: './agents.component.scss'
})
export class AgentsComponent implements OnInit {
  private readonly agentService = inject(AgentService);
  private readonly fb = inject(FormBuilder);

  agents: Agent[] = [];
  loading = false;
  saving = false;
  errorMessage = '';
  editingId: number | null = null;

  formatDateTime = formatDateTime;

  form = this.fb.nonNullable.group({
    agentName: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadAgents();
  }

  loadAgents(): void {
    this.loading = true;
    this.agentService.getAll().subscribe({
      next: (agents) => {
        this.agents = agents;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  startEdit(agent: Agent): void {
    this.editingId = agent.id;
    this.form.patchValue({ agentName: agent.agentName });
    this.errorMessage = '';
  }

  cancelEdit(): void {
    this.editingId = null;
    this.form.reset({ agentName: '' });
    this.errorMessage = '';
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    const name = this.form.controls.agentName.value.trim();

    const request$ = this.editingId
      ? this.agentService.update(this.editingId, name)
      : this.agentService.create(name);

    request$.subscribe({
      next: () => {
        this.saving = false;
        this.cancelEdit();
        this.loadAgents();
      },
      error: (err) => {
        this.saving = false;
        this.errorMessage = err.error?.message ?? 'Có lỗi xảy ra.';
      }
    });
  }

  delete(agent: Agent): void {
    if (!confirm(`Xóa agent "${agent.agentName}"?`)) return;

    this.agentService.delete(agent.id).subscribe({
      next: () => this.loadAgents(),
      error: (err) => alert(err.error?.message ?? 'Không thể xóa agent.')
    });
  }
}
