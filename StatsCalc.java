import java.util.HashMap;
import java.util.Map.Entry;
import java.util.*;
import java.io.*;

class StatsCalc {
	public static void main(String[] args) throws IOException {
		String csvFile = args[0];

		ArrayList<HashMap<String, Integer>> mySets = new ArrayList<>();

		Scanner scn = new Scanner(new File(csvFile));

		String firstLine[] = scn.nextLine().split(",");
		for (int i = 0; i < firstLine.length; i++) {
			mySets.add(new HashMap<>());
		}

		while (scn.hasNextLine()) {
			String line[] = scn.nextLine().split(",");
			for (int i = 0; i < line.length; i++) {
				Integer v = mySets.get(i).get(line[i]);
				mySets.get(i).put(line[i], (v == null ? 0 : v) + 1);
			}
		}
		scn.close();

		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < mySets.size(); i++) {
			sb.append(firstLine[i] + ",\n");
			ArrayList<Entry<String, Integer>> temp = new ArrayList<>(mySets.get(i).entrySet());
			Collections.sort(temp, new Comparator<Entry<String, Integer>>() {
				public int compare(Entry<String, Integer> c1, Entry<String, Integer> c2) {
					try {
						Integer first = Integer.valueOf(c1.getKey());
						Integer second = Integer.valueOf(c2.getKey());

						return first.compareTo(second);
					} catch(NumberFormatException e) {
						return c1.getKey().compareTo(c2.getKey());
					}
				}
			});

			for (Entry<String, Integer> e : temp) {
				sb.append(e.getKey() + "," + e.getValue() + "\n");
			}
			sb.append("\n");
		}

		PrintWriter pw = new PrintWriter(new File("stats.csv"));
		pw.print(sb.toString());
		pw.close();
	}
}