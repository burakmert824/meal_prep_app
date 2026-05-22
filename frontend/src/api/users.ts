import client from './client'
import type { User, CreateUserRequest } from '../types/user'

export const getUsers = async (): Promise<User[]> => {
  const { data } = await client.get<User[]>('/users')
  return data
}

export const createUser = async (req: CreateUserRequest): Promise<User> => {
  const { data } = await client.post<User>('/users', req)
  return data
}