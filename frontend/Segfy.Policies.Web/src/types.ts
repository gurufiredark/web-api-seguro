export type PolicyStatus = "Ativa" | "Cancelada" | "Expirada";

export type Policy = {
  id: string;
  policyNumber: string;
  insuredDocument: string;
  vehiclePlate: string;
  monthlyPremium: number;
  startDate: string;
  endDate: string;
  status: PolicyStatus;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type PolicyPayload = {
  insuredDocument: string;
  vehiclePlate: string;
  monthlyPremium: number;
  startDate: string;
  endDate: string;
  status: PolicyStatus;
};
