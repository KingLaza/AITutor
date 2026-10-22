# Novi ispiti (hold-out)

Ovde idu 2-3 ispita za Operativne sisteme i Programske prevodioce koje sistem nije video, sa rešenjima ako ih ima (npr. od predmetnog profesora).
Fajlovi sa ispitima nisu deo javnog repoa (autorska prava): u `.gitignore` je `eval/exams_new/*` (osim ovog opisa).

Pravila (vidi `thesis/evaluation-protocol.md`):

- Pre prvog pokretanja na ovim zadacima napraviti git tag (`eval-holdout-v1`); pokreće se jednom, bez izmena sistema posle toga.
- Zadaci se prepisuju u `eval/` isto kao `os_questions.py`: tekst zadatka, očekivani odgovor (nezavisno rešenje, ne iz sopstvenog rešavača) i provera.
- Izveštavaju se tri broja odvojeno: pokrivenost šablona, tačnost izdvajanja parametara, tačnost rešavača. Zadaci van šablona se ne izbacuju.
