export interface User {
  id: string
  name: string
  createdAt: string
}

export interface CreateUserRequest {
  name: string
}